using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FoldCanvas
{
    public sealed class FoldCanvasGalleryEntry
    {
        private readonly ReadOnlyCollection<string> tags;

        internal FoldCanvasGalleryEntry(
            string id,
            string title,
            string description,
            string samplePath,
            string foldScriptPath,
            string appearancePath,
            string proofMenuPath,
            string minimumPackageVersion,
            IReadOnlyList<string> sourceTags)
        {
            Id = id;
            Title = title;
            Description = description;
            SamplePath = samplePath;
            FoldScriptPath = foldScriptPath;
            AppearancePath = appearancePath;
            ProofMenuPath = proofMenuPath;
            MinimumPackageVersion = minimumPackageVersion;
            string[] copiedTags = new string[sourceTags.Count];
            for (int i = 0; i < copiedTags.Length; i++)
            {
                copiedTags[i] = sourceTags[i];
            }

            tags = Array.AsReadOnly(copiedTags);
        }

        public string Id { get; }

        public string Title { get; }

        public string Description { get; }

        public string SamplePath { get; }

        public string FoldScriptPath { get; }

        public string AppearancePath { get; }

        public string ProofMenuPath { get; }

        public string MinimumPackageVersion { get; }

        public IReadOnlyList<string> Tags => tags;
    }

    public sealed class FoldCanvasGalleryManifest
    {
        public const string FormatName = "foldcanvas-gallery";
        public const string CurrentVersion = "1";
        public const int MaximumEntryCount = 128;

        private readonly ReadOnlyCollection<FoldCanvasGalleryEntry> entries;

        private FoldCanvasGalleryManifest(
            IReadOnlyList<FoldCanvasGalleryEntry> sourceEntries)
        {
            FoldCanvasGalleryEntry[] copiedEntries =
                new FoldCanvasGalleryEntry[sourceEntries.Count];
            for (int i = 0; i < copiedEntries.Length; i++)
            {
                copiedEntries[i] = sourceEntries[i];
            }

            entries = Array.AsReadOnly(copiedEntries);
        }

        public string Format => FormatName;

        public string Version => CurrentVersion;

        public IReadOnlyList<FoldCanvasGalleryEntry> Entries => entries;

        public static bool TryParse(
            string json,
            out FoldCanvasGalleryManifest manifest,
            out IReadOnlyList<FoldCanvasDiagnostic> diagnostics)
        {
            manifest = null;
            List<FoldCanvasDiagnostic> errors =
                new List<FoldCanvasDiagnostic>();
            diagnostics = errors.AsReadOnly();
            if (!FoldScriptJsonReader.TryParse(
                    json,
                    out FoldScriptJsonValue root,
                    out FoldCanvasDiagnostic parseDiagnostic))
            {
                errors.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.MalformedGalleryManifest,
                    FoldCanvasDiagnosticSeverity.Error,
                    "The FoldCanvas gallery manifest is not valid bounded JSON.",
                    values: parseDiagnostic?.Values));
                return false;
            }

            if (!TryObject(
                    root,
                    new[] { "format", "version", "entries" },
                    out Dictionary<string, FoldScriptJsonValue> rootFields,
                    out string rootError))
            {
                AddMalformed(errors, rootError);
                return false;
            }

            if (!TryString(rootFields, "format", out string format) ||
                !string.Equals(
                    format,
                    FormatName,
                    StringComparison.Ordinal))
            {
                AddMalformed(
                    errors,
                    $"Gallery format must equal '{FormatName}'.");
                return false;
            }

            if (!TryString(rootFields, "version", out string version))
            {
                AddMalformed(errors, "Gallery version must be a string.");
                return false;
            }

            if (!string.Equals(
                    version,
                    CurrentVersion,
                    StringComparison.Ordinal))
            {
                errors.Add(new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.UnsupportedGalleryVersion,
                    FoldCanvasDiagnosticSeverity.Error,
                    $"Gallery version '{version}' is unsupported; expected '{CurrentVersion}'."));
                return false;
            }

            FoldScriptJsonValue entryValue = rootFields["entries"];
            if (entryValue.Kind != FoldScriptJsonKind.Array ||
                entryValue.ArrayValue.Count == 0 ||
                entryValue.ArrayValue.Count > MaximumEntryCount)
            {
                AddMalformed(
                    errors,
                    $"Gallery entries must contain between 1 and {MaximumEntryCount} items.");
                return false;
            }

            List<FoldCanvasGalleryEntry> parsedEntries =
                new List<FoldCanvasGalleryEntry>(
                    entryValue.ArrayValue.Count);
            HashSet<string> ids =
                new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entryValue.ArrayValue.Count; i++)
            {
                if (!TryParseEntry(
                        entryValue.ArrayValue[i],
                        i,
                        errors,
                        out FoldCanvasGalleryEntry entry))
                {
                    return false;
                }

                if (!ids.Add(entry.Id))
                {
                    errors.Add(new FoldCanvasDiagnostic(
                        FoldCanvasDiagnosticCodes
                            .DuplicateGalleryEntryId,
                        FoldCanvasDiagnosticSeverity.Error,
                        $"Gallery entry ID '{entry.Id}' is duplicated.",
                        values: new[]
                        {
                            new FoldCanvasDiagnosticValue(
                                "entryIndex",
                                i)
                        }));
                    return false;
                }

                parsedEntries.Add(entry);
            }

            manifest = new FoldCanvasGalleryManifest(parsedEntries);
            return true;
        }

        private static bool TryParseEntry(
            FoldScriptJsonValue value,
            int entryIndex,
            List<FoldCanvasDiagnostic> diagnostics,
            out FoldCanvasGalleryEntry entry)
        {
            entry = null;
            string[] required =
            {
                "id", "title", "description", "samplePath",
                "minimumPackageVersion", "tags"
            };
            string[] optional =
            {
                "foldScriptPath", "appearancePath", "proofMenuPath"
            };
            if (!TryObject(
                    value,
                    required,
                    optional,
                    out Dictionary<string, FoldScriptJsonValue> fields,
                    out string objectError))
            {
                AddInvalidEntry(diagnostics, entryIndex, objectError);
                return false;
            }

            if (!TryBoundedString(fields, "id", 1, 96, out string id) ||
                !IsKebabIdentifier(id) ||
                !TryBoundedString(fields, "title", 1, 160, out string title) ||
                !TryBoundedString(
                    fields,
                    "description",
                    1,
                    640,
                    out string description) ||
                !TryBoundedString(
                    fields,
                    "samplePath",
                    1,
                    320,
                    out string samplePath) ||
                !IsSafePackagePath(samplePath) ||
                !TryBoundedString(
                    fields,
                    "minimumPackageVersion",
                    1,
                    64,
                    out string minimumVersion) ||
                !IsPackageVersion(minimumVersion))
            {
                AddInvalidEntry(
                    diagnostics,
                    entryIndex,
                    "Gallery entry identity, text, samplePath, or minimumPackageVersion is invalid.");
                return false;
            }

            if (!TryOptionalPackagePath(
                    fields,
                    "foldScriptPath",
                    out string foldScriptPath) ||
                !TryOptionalPackagePath(
                    fields,
                    "appearancePath",
                    out string appearancePath) ||
                !TryOptionalMenuPath(
                    fields,
                    out string proofMenuPath) ||
                !TryTags(
                    fields["tags"],
                    out List<string> tags))
            {
                AddInvalidEntry(
                    diagnostics,
                    entryIndex,
                    "Gallery optional paths, proof menu path, or tags are invalid.");
                return false;
            }

            entry = new FoldCanvasGalleryEntry(
                id,
                title,
                description,
                samplePath,
                foldScriptPath,
                appearancePath,
                proofMenuPath,
                minimumVersion,
                tags);
            return true;
        }

        private static bool TryObject(
            FoldScriptJsonValue value,
            string[] required,
            out Dictionary<string, FoldScriptJsonValue> fields,
            out string error)
        {
            return TryObject(
                value,
                required,
                Array.Empty<string>(),
                out fields,
                out error);
        }

        private static bool TryObject(
            FoldScriptJsonValue value,
            string[] required,
            string[] optional,
            out Dictionary<string, FoldScriptJsonValue> fields,
            out string error)
        {
            fields = null;
            error = null;
            if (value == null || value.Kind != FoldScriptJsonKind.Object)
            {
                error = "Expected a JSON object.";
                return false;
            }

            fields = value.ObjectValue;
            for (int i = 0; i < required.Length; i++)
            {
                if (!fields.ContainsKey(required[i]))
                {
                    error = $"Required property '{required[i]}' is missing.";
                    return false;
                }
            }

            List<string> unknown = new List<string>();
            foreach (string key in fields.Keys)
            {
                if (!Contains(required, key) &&
                    !Contains(optional, key))
                {
                    unknown.Add(key);
                }
            }

            if (unknown.Count > 0)
            {
                unknown.Sort(StringComparer.Ordinal);
                error = $"Unknown property '{unknown[0]}'.";
                return false;
            }

            return true;
        }

        private static bool Contains(string[] values, string candidate)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(
                        values[i],
                        candidate,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryString(
            Dictionary<string, FoldScriptJsonValue> fields,
            string key,
            out string value)
        {
            value = null;
            if (!fields.TryGetValue(
                    key,
                    out FoldScriptJsonValue jsonValue) ||
                jsonValue.Kind != FoldScriptJsonKind.String)
            {
                return false;
            }

            value = jsonValue.StringValue;
            return true;
        }

        private static bool TryBoundedString(
            Dictionary<string, FoldScriptJsonValue> fields,
            string key,
            int minimumLength,
            int maximumLength,
            out string value)
        {
            return TryString(fields, key, out value) &&
                value.Length >= minimumLength &&
                value.Length <= maximumLength &&
                !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryOptionalPackagePath(
            Dictionary<string, FoldScriptJsonValue> fields,
            string key,
            out string value)
        {
            value = null;
            if (!fields.ContainsKey(key))
            {
                return true;
            }

            return TryBoundedString(fields, key, 1, 320, out value) &&
                IsSafePackagePath(value);
        }

        private static bool TryOptionalMenuPath(
            Dictionary<string, FoldScriptJsonValue> fields,
            out string value)
        {
            value = null;
            if (!fields.ContainsKey("proofMenuPath"))
            {
                return true;
            }

            return TryBoundedString(
                    fields,
                    "proofMenuPath",
                    1,
                    240,
                    out value) &&
                value.StartsWith(
                    "Tools/FoldCanvas/",
                    StringComparison.Ordinal) &&
                value.IndexOf("..", StringComparison.Ordinal) < 0;
        }

        private static bool TryTags(
            FoldScriptJsonValue value,
            out List<string> tags)
        {
            tags = null;
            if (value.Kind != FoldScriptJsonKind.Array ||
                value.ArrayValue.Count > 16)
            {
                return false;
            }

            tags = new List<string>(value.ArrayValue.Count);
            HashSet<string> unique =
                new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < value.ArrayValue.Count; i++)
            {
                FoldScriptJsonValue tag = value.ArrayValue[i];
                if (tag.Kind != FoldScriptJsonKind.String ||
                    tag.StringValue.Length == 0 ||
                    tag.StringValue.Length > 48 ||
                    !IsKebabIdentifier(tag.StringValue) ||
                    !unique.Add(tag.StringValue))
                {
                    return false;
                }

                tags.Add(tag.StringValue);
            }

            return true;
        }

        private static bool IsKebabIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value[0] < 'a' || value[0] > 'z' ||
                value[value.Length - 1] == '-')
            {
                return false;
            }

            bool previousDash = false;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                bool isLetter = current >= 'a' && current <= 'z';
                bool isDigit = current >= '0' && current <= '9';
                if (!isLetter && !isDigit && current != '-' ||
                    current == '-' && previousDash)
                {
                    return false;
                }

                previousDash = current == '-';
            }

            return true;
        }

        private static bool IsSafePackagePath(string value)
        {
            if (!value.StartsWith("Samples~/", StringComparison.Ordinal) ||
                value.IndexOf('\\') >= 0 ||
                value.IndexOf(':') >= 0 ||
                value.IndexOf("//", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            string[] segments = value.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length == 0 ||
                    string.Equals(segments[i], ".", StringComparison.Ordinal) ||
                    string.Equals(segments[i], "..", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPackageVersion(string value)
        {
            int dashIndex = value.IndexOf('-');
            string core = dashIndex >= 0
                ? value.Substring(0, dashIndex)
                : value;
            string suffix = dashIndex >= 0
                ? value.Substring(dashIndex + 1)
                : string.Empty;
            string[] parts = core.Split('.');
            if (parts.Length != 3 ||
                dashIndex >= 0 && suffix.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int number) ||
                    number < 0 ||
                    number.ToString(CultureInfo.InvariantCulture) != parts[i])
                {
                    return false;
                }
            }

            for (int i = 0; i < suffix.Length; i++)
            {
                char current = suffix[i];
                bool valid = current >= 'a' && current <= 'z' ||
                    current >= 'A' && current <= 'Z' ||
                    current >= '0' && current <= '9' ||
                    current == '.' || current == '-';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddMalformed(
            List<FoldCanvasDiagnostic> diagnostics,
            string message)
        {
            diagnostics.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.MalformedGalleryManifest,
                FoldCanvasDiagnosticSeverity.Error,
                message));
        }

        private static void AddInvalidEntry(
            List<FoldCanvasDiagnostic> diagnostics,
            int entryIndex,
            string message)
        {
            diagnostics.Add(new FoldCanvasDiagnostic(
                FoldCanvasDiagnosticCodes.InvalidGalleryEntry,
                FoldCanvasDiagnosticSeverity.Error,
                message,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "entryIndex",
                        entryIndex)
                }));
        }
    }
}
