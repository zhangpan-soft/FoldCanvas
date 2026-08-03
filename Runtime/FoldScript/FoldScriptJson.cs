using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FoldCanvas
{
    internal enum FoldScriptJsonKind
    {
        Null,
        Boolean,
        Number,
        String,
        Array,
        Object
    }

    internal sealed class FoldScriptJsonValue
    {
        private FoldScriptJsonValue(FoldScriptJsonKind kind)
        {
            Kind = kind;
        }

        public FoldScriptJsonKind Kind { get; }

        public bool BooleanValue { get; private set; }

        public double NumberValue { get; private set; }

        public string StringValue { get; private set; }

        public List<FoldScriptJsonValue> ArrayValue { get; private set; }

        public Dictionary<string, FoldScriptJsonValue> ObjectValue
        {
            get;
            private set;
        }

        public static FoldScriptJsonValue Null()
        {
            return new FoldScriptJsonValue(FoldScriptJsonKind.Null);
        }

        public static FoldScriptJsonValue Boolean(bool value)
        {
            return new FoldScriptJsonValue(FoldScriptJsonKind.Boolean)
            {
                BooleanValue = value
            };
        }

        public static FoldScriptJsonValue Number(double value)
        {
            return new FoldScriptJsonValue(FoldScriptJsonKind.Number)
            {
                NumberValue = value
            };
        }

        public static FoldScriptJsonValue String(string value)
        {
            return new FoldScriptJsonValue(FoldScriptJsonKind.String)
            {
                StringValue = value
            };
        }

        public static FoldScriptJsonValue Array(
            List<FoldScriptJsonValue> value)
        {
            return new FoldScriptJsonValue(FoldScriptJsonKind.Array)
            {
                ArrayValue = value
            };
        }

        public static FoldScriptJsonValue Object(
            Dictionary<string, FoldScriptJsonValue> value)
        {
            return new FoldScriptJsonValue(FoldScriptJsonKind.Object)
            {
                ObjectValue = value
            };
        }
    }

    internal static class FoldScriptJsonReader
    {
        public static bool TryParse(
            string json,
            out FoldScriptJsonValue value,
            out FoldCanvasDiagnostic diagnostic)
        {
            value = null;
            diagnostic = null;
            if (json == null)
            {
                diagnostic = CreateDiagnostic(
                    FoldCanvasDiagnosticCodes.MalformedFoldScript,
                    "FoldScript JSON cannot be null.",
                    0);
                return false;
            }

            if (json.Length > FoldCanvasLimits.MaximumFoldScriptCharacters)
            {
                diagnostic = new FoldCanvasDiagnostic(
                    FoldCanvasDiagnosticCodes.FoldScriptInputLimitExceeded,
                    FoldCanvasDiagnosticSeverity.Error,
                    "FoldScript JSON exceeds the maximum input size before parsing.",
                    values: new[]
                    {
                        new FoldCanvasDiagnosticValue(
                            "characterCount",
                            json.Length),
                        new FoldCanvasDiagnosticValue(
                            "maximumCharacterCount",
                            FoldCanvasLimits.MaximumFoldScriptCharacters)
                    });
                return false;
            }

            Parser parser = new Parser(json);
            try
            {
                value = parser.Parse();
                return true;
            }
            catch (FoldScriptJsonException exception)
            {
                diagnostic = CreateDiagnostic(
                    exception.Code,
                    exception.Message,
                    exception.CharacterIndex);
                return false;
            }
        }

        private static FoldCanvasDiagnostic CreateDiagnostic(
            string code,
            string message,
            int characterIndex)
        {
            return new FoldCanvasDiagnostic(
                code,
                FoldCanvasDiagnosticSeverity.Error,
                message,
                values: new[]
                {
                    new FoldCanvasDiagnosticValue(
                        "characterIndex",
                        characterIndex)
                });
        }

        private sealed class Parser
        {
            private readonly string json;
            private int index;
            private int nodeCount;

            public Parser(string source)
            {
                json = source;
            }

            public FoldScriptJsonValue Parse()
            {
                SkipWhitespace();
                FoldScriptJsonValue result = ParseValue(0);
                SkipWhitespace();
                if (index != json.Length)
                {
                    ThrowMalformed("Unexpected trailing JSON content.");
                }

                return result;
            }

            private FoldScriptJsonValue ParseValue(int depth)
            {
                if (depth > FoldCanvasLimits.MaximumFoldScriptJsonDepth)
                {
                    throw new FoldScriptJsonException(
                        FoldCanvasDiagnosticCodes
                            .FoldScriptInputLimitExceeded,
                        "FoldScript JSON exceeds the maximum nesting depth.",
                        index);
                }

                nodeCount++;
                if (nodeCount > FoldCanvasLimits.MaximumFoldScriptJsonNodes)
                {
                    throw new FoldScriptJsonException(
                        FoldCanvasDiagnosticCodes
                            .FoldScriptInputLimitExceeded,
                        "FoldScript JSON exceeds the maximum node count.",
                        index);
                }

                SkipWhitespace();
                if (index >= json.Length)
                {
                    ThrowMalformed("Unexpected end of FoldScript JSON.");
                }

                char current = json[index];
                switch (current)
                {
                    case '{':
                        return ParseObject(depth + 1);
                    case '[':
                        return ParseArray(depth + 1);
                    case '"':
                        return FoldScriptJsonValue.String(ParseString());
                    case 't':
                        ConsumeLiteral("true");
                        return FoldScriptJsonValue.Boolean(true);
                    case 'f':
                        ConsumeLiteral("false");
                        return FoldScriptJsonValue.Boolean(false);
                    case 'n':
                        ConsumeLiteral("null");
                        return FoldScriptJsonValue.Null();
                    case 'N':
                    case 'I':
                        ThrowNonFinite();
                        return null;
                    default:
                        if (current == '-' &&
                            index + 1 < json.Length &&
                            (json[index + 1] == 'I' ||
                             json[index + 1] == 'N'))
                        {
                            ThrowNonFinite();
                        }

                        if (current == '-' || IsDigit(current))
                        {
                            return FoldScriptJsonValue.Number(ParseNumber());
                        }

                        ThrowMalformed(
                            $"Unexpected character '{current}' in FoldScript JSON.");
                        return null;
                }
            }

            private FoldScriptJsonValue ParseObject(int depth)
            {
                index++;
                SkipWhitespace();
                Dictionary<string, FoldScriptJsonValue> values =
                    new Dictionary<string, FoldScriptJsonValue>(
                        StringComparer.Ordinal);
                if (TryConsume('}'))
                {
                    return FoldScriptJsonValue.Object(values);
                }

                while (true)
                {
                    SkipWhitespace();
                    if (index >= json.Length || json[index] != '"')
                    {
                        ThrowMalformed("JSON object property names must be strings.");
                    }

                    int propertyIndex = index;
                    string propertyName = ParseString();
                    if (values.ContainsKey(propertyName))
                    {
                        throw new FoldScriptJsonException(
                            FoldCanvasDiagnosticCodes
                                .DuplicateFoldScriptProperty,
                            $"FoldScript JSON contains duplicate property '{propertyName}'.",
                            propertyIndex);
                    }

                    SkipWhitespace();
                    Require(':');
                    FoldScriptJsonValue propertyValue = ParseValue(depth);
                    values.Add(propertyName, propertyValue);
                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        break;
                    }

                    Require(',');
                }

                return FoldScriptJsonValue.Object(values);
            }

            private FoldScriptJsonValue ParseArray(int depth)
            {
                index++;
                SkipWhitespace();
                List<FoldScriptJsonValue> values =
                    new List<FoldScriptJsonValue>();
                if (TryConsume(']'))
                {
                    return FoldScriptJsonValue.Array(values);
                }

                while (true)
                {
                    values.Add(ParseValue(depth));
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        break;
                    }

                    Require(',');
                }

                return FoldScriptJsonValue.Array(values);
            }

            private string ParseString()
            {
                Require('"');
                StringBuilder builder = new StringBuilder();
                while (index < json.Length)
                {
                    char current = json[index++];
                    if (current == '"')
                    {
                        return builder.ToString();
                    }

                    if (current < 0x20)
                    {
                        ThrowMalformed(
                            "JSON strings cannot contain unescaped control characters.");
                    }

                    if (current == '\\')
                    {
                        if (index >= json.Length)
                        {
                            ThrowMalformed("Incomplete JSON string escape.");
                        }

                        char escaped = json[index++];
                        switch (escaped)
                        {
                            case '"': builder.Append('"'); break;
                            case '\\': builder.Append('\\'); break;
                            case '/': builder.Append('/'); break;
                            case 'b': builder.Append('\b'); break;
                            case 'f': builder.Append('\f'); break;
                            case 'n': builder.Append('\n'); break;
                            case 'r': builder.Append('\r'); break;
                            case 't': builder.Append('\t'); break;
                            case 'u': builder.Append(ParseUnicodeEscape()); break;
                            default:
                                ThrowMalformed(
                                    $"Unsupported JSON escape '\\{escaped}'.");
                                break;
                        }
                    }
                    else
                    {
                        builder.Append(current);
                    }

                    if (builder.Length >
                        FoldCanvasLimits.MaximumFoldScriptStringLength)
                    {
                        throw new FoldScriptJsonException(
                            FoldCanvasDiagnosticCodes
                                .FoldScriptInputLimitExceeded,
                            "FoldScript JSON contains a string above the configured limit.",
                            index);
                    }
                }

                ThrowMalformed("Unterminated JSON string.");
                return null;
            }

            private char ParseUnicodeEscape()
            {
                if (index > json.Length - 4)
                {
                    ThrowMalformed("Incomplete JSON unicode escape.");
                }

                int value = 0;
                for (int i = 0; i < 4; i++)
                {
                    int digit = HexValue(json[index++]);
                    if (digit < 0)
                    {
                        ThrowMalformed("Invalid JSON unicode escape.");
                    }

                    value = (value << 4) | digit;
                }

                return (char)value;
            }

            private double ParseNumber()
            {
                int start = index;
                if (TryConsume('-') && index >= json.Length)
                {
                    ThrowMalformed("Incomplete JSON number.");
                }

                if (TryConsume('0'))
                {
                    if (index < json.Length && IsDigit(json[index]))
                    {
                        ThrowMalformed("JSON numbers cannot contain leading zeros.");
                    }
                }
                else
                {
                    RequireDigits();
                }

                if (TryConsume('.'))
                {
                    RequireDigits();
                }

                if (index < json.Length &&
                    (json[index] == 'e' || json[index] == 'E'))
                {
                    index++;
                    if (index < json.Length &&
                        (json[index] == '+' || json[index] == '-'))
                    {
                        index++;
                    }

                    RequireDigits();
                }

                string token = json.Substring(start, index - start);
                if (!double.TryParse(
                        token,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double value))
                {
                    ThrowMalformed("Invalid JSON number.");
                }

                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    ThrowNonFinite(start);
                }

                return value;
            }

            private void RequireDigits()
            {
                int start = index;
                while (index < json.Length && IsDigit(json[index]))
                {
                    index++;
                }

                if (start == index)
                {
                    ThrowMalformed("JSON number is missing required digits.");
                }
            }

            private void ConsumeLiteral(string literal)
            {
                if (index > json.Length - literal.Length ||
                    string.CompareOrdinal(
                        json,
                        index,
                        literal,
                        0,
                        literal.Length) != 0)
                {
                    ThrowMalformed($"Invalid JSON literal near '{literal}'.");
                }

                index += literal.Length;
            }

            private void Require(char expected)
            {
                SkipWhitespace();
                if (!TryConsume(expected))
                {
                    ThrowMalformed($"Expected '{expected}' in FoldScript JSON.");
                }
            }

            private bool TryConsume(char expected)
            {
                if (index < json.Length && json[index] == expected)
                {
                    index++;
                    return true;
                }

                return false;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length)
                {
                    char current = json[index];
                    if (current != ' ' && current != '\t' &&
                        current != '\r' && current != '\n')
                    {
                        break;
                    }

                    index++;
                }
            }

            private void ThrowMalformed(string message)
            {
                throw new FoldScriptJsonException(
                    FoldCanvasDiagnosticCodes.MalformedFoldScript,
                    message,
                    index);
            }

            private void ThrowNonFinite(int? at = null)
            {
                throw new FoldScriptJsonException(
                    FoldCanvasDiagnosticCodes.NonFiniteFoldScriptNumber,
                    "FoldScript numbers must be finite standard JSON numbers; NaN and Infinity are forbidden.",
                    at ?? index);
            }

            private static bool IsDigit(char value)
            {
                return value >= '0' && value <= '9';
            }

            private static int HexValue(char value)
            {
                if (value >= '0' && value <= '9')
                {
                    return value - '0';
                }

                if (value >= 'a' && value <= 'f')
                {
                    return value - 'a' + 10;
                }

                if (value >= 'A' && value <= 'F')
                {
                    return value - 'A' + 10;
                }

                return -1;
            }
        }
    }

    internal sealed class FoldScriptJsonException : Exception
    {
        public FoldScriptJsonException(
            string code,
            string message,
            int characterIndex)
            : base(message)
        {
            Code = code;
            CharacterIndex = characterIndex;
        }

        public string Code { get; }

        public int CharacterIndex { get; }
    }

    internal static class FoldScriptJsonCanonicalWriter
    {
        public static string Write(FoldScriptJsonValue value)
        {
            StringBuilder builder = new StringBuilder();
            AppendValue(builder, value);
            return builder.ToString();
        }

        public static void AppendEscapedString(
            StringBuilder builder,
            string value)
        {
            builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                switch (current)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (current < 0x20 || current == '\u2028' ||
                            current == '\u2029')
                        {
                            builder.Append("\\u");
                            builder.Append(
                                ((int)current).ToString(
                                    "x4",
                                    CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(current);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        public static void AppendNumber(StringBuilder builder, double value)
        {
            if (value == 0d)
            {
                builder.Append('0');
                return;
            }

            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        public static void AppendNumber(StringBuilder builder, float value)
        {
            if (value == 0f)
            {
                builder.Append('0');
                return;
            }

            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void AppendValue(
            StringBuilder builder,
            FoldScriptJsonValue value)
        {
            switch (value.Kind)
            {
                case FoldScriptJsonKind.Null:
                    builder.Append("null");
                    return;
                case FoldScriptJsonKind.Boolean:
                    builder.Append(value.BooleanValue ? "true" : "false");
                    return;
                case FoldScriptJsonKind.Number:
                    AppendNumber(builder, value.NumberValue);
                    return;
                case FoldScriptJsonKind.String:
                    AppendEscapedString(builder, value.StringValue);
                    return;
                case FoldScriptJsonKind.Array:
                    builder.Append('[');
                    for (int i = 0; i < value.ArrayValue.Count; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(',');
                        }

                        AppendValue(builder, value.ArrayValue[i]);
                    }

                    builder.Append(']');
                    return;
                case FoldScriptJsonKind.Object:
                    builder.Append('{');
                    List<string> keys =
                        new List<string>(value.ObjectValue.Keys);
                    keys.Sort(StringComparer.Ordinal);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(',');
                        }

                        AppendEscapedString(builder, keys[i]);
                        builder.Append(':');
                        AppendValue(builder, value.ObjectValue[keys[i]]);
                    }

                    builder.Append('}');
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
