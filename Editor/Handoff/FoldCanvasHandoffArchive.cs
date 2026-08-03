using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FoldCanvas.Editor
{
    internal sealed class FoldCanvasHandoffArchiveContent
    {
        public string ArchiveSha256 { get; set; }

        public Dictionary<string, byte[]> Entries { get; } =
            new Dictionary<string, byte[]>(StringComparer.Ordinal);
    }

    internal static class FoldCanvasHandoffArchive
    {
        private const uint LocalHeaderSignature = 0x04034b50;

        private const uint CentralHeaderSignature = 0x02014b50;

        private const uint EndOfCentralDirectorySignature = 0x06054b50;

        private const ushort Utf8Flag = 0x0800;

        private const ushort StoredMethod = 0;

        private const ushort DosTime = 0;

        private const ushort DosDate = 33;

        private static readonly uint[] CrcTable = BuildCrcTable();

        public static void Write(
            Stream output,
            IReadOnlyDictionary<string, byte[]> entries)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (!output.CanWrite || !output.CanSeek)
            {
                throw new ArgumentException(
                    "Handoff ZIP output must be writable and seekable.",
                    nameof(output));
            }

            List<CentralEntry> centralEntries =
                new List<CentralEntry>(
                    FoldCanvasHandoffFormat.OrderedEntries.Count);
            using (BinaryWriter writer = new BinaryWriter(
                output,
                new UTF8Encoding(false),
                true))
            {
                for (int i = 0;
                    i < FoldCanvasHandoffFormat.OrderedEntries.Count;
                    i++)
                {
                    string name =
                        FoldCanvasHandoffFormat.OrderedEntries[i];
                    if (!entries.TryGetValue(name, out byte[] bytes) ||
                        bytes == null)
                    {
                        throw new InvalidDataException(
                            $"Missing handoff archive entry '{name}'.");
                    }

                    long maximum =
                        FoldCanvasHandoffLimits.MaximumBytesForEntry(name);
                    if (bytes.LongLength > maximum)
                    {
                        throw new InvalidDataException(
                            $"Handoff archive entry '{name}' exceeds its byte limit.");
                    }

                    byte[] nameBytes = new UTF8Encoding(false, true)
                        .GetBytes(name);
                    if (bytes.LongLength > uint.MaxValue ||
                        output.Position > uint.MaxValue)
                    {
                        throw new InvalidDataException(
                            "Handoff archive exceeds ZIP32 limits.");
                    }

                    uint crc = ComputeCrc32(bytes);
                    uint length = checked((uint)bytes.Length);
                    uint localOffset = checked((uint)output.Position);
                    writer.Write(LocalHeaderSignature);
                    writer.Write((ushort)20);
                    writer.Write(Utf8Flag);
                    writer.Write(StoredMethod);
                    writer.Write(DosTime);
                    writer.Write(DosDate);
                    writer.Write(crc);
                    writer.Write(length);
                    writer.Write(length);
                    writer.Write(checked((ushort)nameBytes.Length));
                    writer.Write((ushort)0);
                    writer.Write(nameBytes);
                    writer.Write(bytes);
                    centralEntries.Add(new CentralEntry(
                        nameBytes,
                        crc,
                        length,
                        localOffset));
                }

                if (output.Position > uint.MaxValue)
                {
                    throw new InvalidDataException(
                        "Handoff archive exceeds ZIP32 limits.");
                }

                uint centralOffset = checked((uint)output.Position);
                for (int i = 0; i < centralEntries.Count; i++)
                {
                    CentralEntry entry = centralEntries[i];
                    writer.Write(CentralHeaderSignature);
                    writer.Write((ushort)20);
                    writer.Write((ushort)20);
                    writer.Write(Utf8Flag);
                    writer.Write(StoredMethod);
                    writer.Write(DosTime);
                    writer.Write(DosDate);
                    writer.Write(entry.Crc32);
                    writer.Write(entry.Length);
                    writer.Write(entry.Length);
                    writer.Write(checked((ushort)entry.NameBytes.Length));
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)0);
                    writer.Write(entry.LocalOffset);
                    writer.Write(entry.NameBytes);
                }

                uint centralSize = checked(
                    (uint)(output.Position - centralOffset));
                writer.Write(EndOfCentralDirectorySignature);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write(checked((ushort)centralEntries.Count));
                writer.Write(checked((ushort)centralEntries.Count));
                writer.Write(centralSize);
                writer.Write(centralOffset);
                writer.Write((ushort)0);
                writer.Flush();
            }
        }

        public static bool TryRead(
            string archivePath,
            out FoldCanvasHandoffArchiveContent content,
            out FoldCanvasDiagnostic diagnostic)
        {
            content = null;
            diagnostic = null;
            if (string.IsNullOrWhiteSpace(archivePath) ||
                !File.Exists(archivePath))
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Handoff import requires an existing archive file.");
                return false;
            }

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(archivePath);
            }
            catch (Exception exception)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffInputMissing,
                    "Handoff archive metadata could not be read: " +
                    exception.Message);
                return false;
            }

            if (fileInfo.Length <= 0 ||
                fileInfo.Length > FoldCanvasHandoffLimits.MaximumArchiveBytes)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffLimitExceeded,
                    "Handoff archive byte length is outside the supported limit.",
                    new FoldCanvasDiagnosticValue(
                        "archiveBytes",
                        Math.Max(0L, fileInfo.Length)),
                    new FoldCanvasDiagnosticValue(
                        "maximumArchiveBytes",
                        FoldCanvasHandoffLimits.MaximumArchiveBytes));
                return false;
            }

            try
            {
                using (FileStream stream = new FileStream(
                    archivePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    if (!TryValidateCentralDirectory(stream, out diagnostic))
                    {
                        return false;
                    }

                    stream.Position = 0;
                    FoldCanvasHandoffArchiveContent result =
                        new FoldCanvasHandoffArchiveContent
                        {
                            ArchiveSha256 =
                                FoldCanvasHandoffHash.File(archivePath),
                        };
                    using (ZipArchive archive = new ZipArchive(
                        stream,
                        ZipArchiveMode.Read,
                        true,
                        new UTF8Encoding(false)))
                    {
                        if (archive.Entries.Count !=
                            FoldCanvasHandoffLimits.EntryCount)
                        {
                            diagnostic = Unsafe(
                                "Handoff archive must contain exactly six entries.");
                            return false;
                        }

                        long expandedBytes = 0L;
                        for (int i = 0; i < archive.Entries.Count; i++)
                        {
                            ZipArchiveEntry entry = archive.Entries[i];
                            string expected =
                                FoldCanvasHandoffFormat.OrderedEntries[i];
                            if (!string.Equals(
                                    entry.FullName,
                                    expected,
                                    StringComparison.Ordinal) ||
                                !IsSafeEntryName(entry.FullName) ||
                                IsLink(entry.ExternalAttributes) ||
                                entry.CompressedLength != entry.Length)
                            {
                                diagnostic = Unsafe(
                                    "Handoff archive entries must use the exact stored version 1 layout.");
                                return false;
                            }

                            if (!HasFixedTimestamp(entry.LastWriteTime))
                            {
                                diagnostic = Unsafe(
                                    $"Handoff entry '{entry.FullName}' has noncanonical metadata.");
                                return false;
                            }

                            long maximum =
                                FoldCanvasHandoffLimits.MaximumBytesForEntry(
                                    entry.FullName);
                            if (entry.Length < 0 || entry.Length > maximum)
                            {
                                diagnostic = EntryLimit(
                                    entry.FullName,
                                    entry.Length,
                                    maximum);
                                return false;
                            }

                            expandedBytes = checked(
                                expandedBytes + entry.Length);
                            if (expandedBytes >
                                FoldCanvasHandoffLimits.MaximumExpandedBytes)
                            {
                                diagnostic = Error(
                                    FoldCanvasDiagnosticCodes
                                        .HandoffLimitExceeded,
                                    "Handoff expanded bytes exceed the total safety limit.",
                                    new FoldCanvasDiagnosticValue(
                                        "expandedBytes",
                                        expandedBytes),
                                    new FoldCanvasDiagnosticValue(
                                        "maximumExpandedBytes",
                                        FoldCanvasHandoffLimits
                                            .MaximumExpandedBytes));
                                return false;
                            }

                            byte[] bytes = new byte[checked((int)entry.Length)];
                            using (Stream entryStream = entry.Open())
                            {
                                int offset = 0;
                                while (offset < bytes.Length)
                                {
                                    int read = entryStream.Read(
                                        bytes,
                                        offset,
                                        bytes.Length - offset);
                                    if (read <= 0)
                                    {
                                        diagnostic = Error(
                                            FoldCanvasDiagnosticCodes
                                                .HandoffIntegrityMismatch,
                                            $"Handoff entry '{entry.FullName}' ended before its declared length.");
                                        return false;
                                    }

                                    offset += read;
                                }

                                if (entryStream.ReadByte() >= 0)
                                {
                                    diagnostic = Error(
                                        FoldCanvasDiagnosticCodes
                                            .HandoffIntegrityMismatch,
                                        $"Handoff entry '{entry.FullName}' exceeded its declared length.");
                                    return false;
                                }
                            }

                            result.Entries.Add(entry.FullName, bytes);
                        }
                    }

                    content = result;
                    return true;
                }
            }
            catch (InvalidDataException exception)
            {
                diagnostic = Unsafe(
                    "Handoff archive is not a valid stored ZIP: " +
                    exception.Message);
                return false;
            }
            catch (OverflowException)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffLimitExceeded,
                    "Handoff archive size arithmetic exceeded supported limits.");
                return false;
            }
            catch (Exception exception)
            {
                diagnostic = Error(
                    FoldCanvasDiagnosticCodes.HandoffIntegrityMismatch,
                    "Handoff archive could not be read: " + exception.Message);
                return false;
            }
        }

        private static bool TryValidateCentralDirectory(
            FileStream stream,
            out FoldCanvasDiagnostic diagnostic)
        {
            diagnostic = null;
            long tailLength = Math.Min(stream.Length, 65557L);
            byte[] tail = new byte[checked((int)tailLength)];
            stream.Position = stream.Length - tailLength;
            ReadExactly(stream, tail, 0, tail.Length);
            int endOffset = FindSignatureFromEnd(
                tail,
                EndOfCentralDirectorySignature);
            if (endOffset < 0 || endOffset + 22 > tail.Length)
            {
                diagnostic = Unsafe(
                    "Handoff archive has no valid central directory terminator.");
                return false;
            }

            ushort disk = UInt16(tail, endOffset + 4);
            ushort centralDisk = UInt16(tail, endOffset + 6);
            ushort entriesOnDisk = UInt16(tail, endOffset + 8);
            ushort totalEntries = UInt16(tail, endOffset + 10);
            uint centralSize = UInt32(tail, endOffset + 12);
            uint centralOffset = UInt32(tail, endOffset + 16);
            ushort commentLength = UInt16(tail, endOffset + 20);
            long absoluteEndOffset =
                stream.Length - tailLength + endOffset;
            if (disk != 0 || centralDisk != 0 ||
                entriesOnDisk != FoldCanvasHandoffLimits.EntryCount ||
                totalEntries != FoldCanvasHandoffLimits.EntryCount ||
                commentLength != 0 ||
                absoluteEndOffset + 22 != stream.Length ||
                (long)centralOffset + centralSize != absoluteEndOffset)
            {
                diagnostic = Unsafe(
                    "Handoff archive central directory is noncanonical or uses unsupported ZIP features.");
                return false;
            }

            stream.Position = centralOffset;
            long centralEnd = (long)centralOffset + centralSize;
            byte[] header = new byte[46];
            for (int i = 0; i < totalEntries; i++)
            {
                ReadExactly(stream, header, 0, header.Length);
                if (UInt32(header, 0) != CentralHeaderSignature)
                {
                    diagnostic = Unsafe(
                        "Handoff archive central directory contains an invalid entry header.");
                    return false;
                }

                ushort flags = UInt16(header, 8);
                ushort method = UInt16(header, 10);
                uint compressed = UInt32(header, 20);
                uint expanded = UInt32(header, 24);
                ushort nameLength = UInt16(header, 28);
                ushort extraLength = UInt16(header, 30);
                ushort entryCommentLength = UInt16(header, 32);
                uint externalAttributes = UInt32(header, 38);
                uint localOffset = UInt32(header, 42);
                if ((flags & 0x0001) != 0 || method != 0 ||
                    compressed != expanded ||
                    nameLength == 0 || nameLength > 512 ||
                    extraLength != 0 || entryCommentLength != 0 ||
                    IsLink(unchecked((int)externalAttributes)) ||
                    localOffset >= centralOffset)
                {
                    diagnostic = Unsafe(
                        "Handoff archive entries must be unencrypted, stored, unlinked, and metadata-free.");
                    return false;
                }

                byte[] nameBytes = new byte[nameLength];
                ReadExactly(stream, nameBytes, 0, nameBytes.Length);
                string name = new UTF8Encoding(false, true).GetString(nameBytes);
                if (!string.Equals(
                        name,
                        FoldCanvasHandoffFormat.OrderedEntries[i],
                        StringComparison.Ordinal) ||
                    !IsSafeEntryName(name))
                {
                    diagnostic = Unsafe(
                        "Handoff archive entry names or order do not match version 1.");
                    return false;
                }

                long maximum =
                    FoldCanvasHandoffLimits.MaximumBytesForEntry(name);
                if (expanded > maximum)
                {
                    diagnostic = EntryLimit(name, expanded, maximum);
                    return false;
                }
            }

            if (stream.Position != centralEnd)
            {
                diagnostic = Unsafe(
                    "Handoff archive central directory length is inconsistent.");
                return false;
            }

            return true;
        }

        private static bool IsSafeEntryName(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value[0] == '/' ||
                value.IndexOf('\\') >= 0 ||
                value.EndsWith("/", StringComparison.Ordinal) ||
                value.IndexOf(':') >= 0)
            {
                return false;
            }

            string[] segments = value.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length == 0 ||
                    segments[i] == "." ||
                    segments[i] == "..")
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLink(int externalAttributes)
        {
            int unixType = (externalAttributes >> 16) & 0xF000;
            bool unixLink = unixType == 0xA000;
            bool windowsReparse =
                (externalAttributes & 0x00000400) != 0;
            return unixLink || windowsReparse;
        }

        private static bool HasFixedTimestamp(DateTimeOffset value)
        {
            return value.Year == 1980 &&
                value.Month == 1 &&
                value.Day == 1 &&
                value.Hour == 0 &&
                value.Minute == 0 &&
                value.Second == 0;
        }

        private static int FindSignatureFromEnd(byte[] bytes, uint signature)
        {
            for (int i = bytes.Length - 4; i >= 0; i--)
            {
                if (UInt32(bytes, i) == signature)
                {
                    return i;
                }
            }

            return -1;
        }

        private static ushort UInt16(byte[] bytes, int offset)
        {
            return (ushort)(bytes[offset] | bytes[offset + 1] << 8);
        }

        private static uint UInt32(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset] |
                bytes[offset + 1] << 8 |
                bytes[offset + 2] << 16 |
                bytes[offset + 3] << 24);
        }

        private static void ReadExactly(
            Stream stream,
            byte[] bytes,
            int offset,
            int count)
        {
            while (count > 0)
            {
                int read = stream.Read(bytes, offset, count);
                if (read <= 0)
                {
                    throw new InvalidDataException(
                        "Unexpected end of ZIP metadata.");
                }

                offset += read;
                count -= read;
            }
        }

        private static uint ComputeCrc32(byte[] bytes)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < bytes.Length; i++)
            {
                crc = CrcTable[(crc ^ bytes[i]) & 0xFF] ^ (crc >> 8);
            }

            return crc ^ 0xFFFFFFFFu;
        }

        private static uint[] BuildCrcTable()
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                uint value = i;
                for (int bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) != 0
                        ? 0xEDB88320u ^ (value >> 1)
                        : value >> 1;
                }

                table[i] = value;
            }

            return table;
        }

        private sealed class CentralEntry
        {
            public CentralEntry(
                byte[] nameBytes,
                uint crc32,
                uint length,
                uint localOffset)
            {
                NameBytes = nameBytes;
                Crc32 = crc32;
                Length = length;
                LocalOffset = localOffset;
            }

            public byte[] NameBytes { get; }

            public uint Crc32 { get; }

            public uint Length { get; }

            public uint LocalOffset { get; }
        }

        private static FoldCanvasDiagnostic Unsafe(string message)
        {
            return Error(
                FoldCanvasDiagnosticCodes.UnsafeHandoffEntry,
                message);
        }

        private static FoldCanvasDiagnostic EntryLimit(
            string name,
            long actual,
            long maximum)
        {
            return Error(
                FoldCanvasDiagnosticCodes.HandoffLimitExceeded,
                $"Handoff entry '{name}' exceeds its safety limit.",
                new FoldCanvasDiagnosticValue("entryBytes", actual),
                new FoldCanvasDiagnosticValue("maximumEntryBytes", maximum));
        }

        private static FoldCanvasDiagnostic Error(
            string code,
            string message,
            params FoldCanvasDiagnosticValue[] values)
        {
            return new FoldCanvasDiagnostic(
                code,
                FoldCanvasDiagnosticSeverity.Error,
                message,
                values: values);
        }
    }
}
