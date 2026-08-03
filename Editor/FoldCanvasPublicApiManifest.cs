using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using PackageManagerInfo = UnityEditor.PackageManager.PackageInfo;

namespace FoldCanvas.Editor
{
    [Serializable]
    internal sealed class FoldCanvasPublicApiManifestDocument
    {
        public string format;
        public string version;
        public string assembly;
        public string packageVersion;
        public int signatureCount;
        public string sha256;
        public string[] signatures;
    }

    public static class FoldCanvasPublicApiManifest
    {
        internal const string Format = "foldcanvas-public-runtime-api";
        internal const string ManifestVersion = "1";
        internal const string AssemblyName = "FoldCanvas.Runtime";
        internal const string RelativeBaselinePath =
            "Documentation~/public-runtime-api.json";

        [MenuItem(
            "Tools/FoldCanvas/Maintenance/Regenerate Public Runtime API Baseline")]
        public static void RegenerateBaseline()
        {
            Assembly runtimeAssembly = typeof(FoldCanvasCompiler).Assembly;
            PackageManagerInfo package =
                PackageManagerInfo.FindForAssembly(runtimeAssembly);
            if (package == null)
            {
                throw new InvalidOperationException(
                    "Could not resolve the FoldCanvas package root.");
            }

            string baselinePath = Path.Combine(
                package.resolvedPath,
                RelativeBaselinePath);
            File.WriteAllText(
                baselinePath,
                GenerateJson(runtimeAssembly, FoldCanvasVersion.Package),
                new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log(
                "FoldCanvas public Runtime API baseline regenerated at " +
                baselinePath + ".");
        }

        internal static string GenerateJson(
            Assembly assembly,
            string packageVersion)
        {
            FoldCanvasPublicApiManifestDocument document = CreateDocument(
                assembly,
                packageVersion);
            StringBuilder builder = new StringBuilder();
            builder.Append("{\n");
            AppendProperty(builder, "format", document.format, true);
            AppendProperty(builder, "version", document.version, true);
            AppendProperty(builder, "assembly", document.assembly, true);
            AppendProperty(
                builder,
                "packageVersion",
                document.packageVersion,
                true);
            builder.Append("  \"signatureCount\": ");
            builder.Append(
                document.signatureCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(",\n");
            AppendProperty(builder, "sha256", document.sha256, true);
            builder.Append("  \"signatures\": [\n");
            for (int i = 0; i < document.signatures.Length; i++)
            {
                builder.Append("    \"");
                builder.Append(EscapeJson(document.signatures[i]));
                builder.Append(i + 1 == document.signatures.Length
                    ? "\"\n"
                    : "\",\n");
            }

            builder.Append("  ]\n");
            builder.Append("}\n");
            return builder.ToString();
        }

        internal static FoldCanvasPublicApiManifestDocument CreateDocument(
            Assembly assembly,
            string packageVersion)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new ArgumentException(
                    "Package version must be non-empty.",
                    nameof(packageVersion));
            }

            string[] signatures = CreateSignatures(assembly).ToArray();
            return new FoldCanvasPublicApiManifestDocument
            {
                format = Format,
                version = ManifestVersion,
                assembly = AssemblyName,
                packageVersion = packageVersion,
                signatureCount = signatures.Length,
                sha256 = ComputeSignatureDigest(signatures),
                signatures = signatures,
            };
        }

        internal static IReadOnlyList<string> CreateSignatures(
            Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            List<string> signatures = new List<string>();
            Type[] types = assembly
                .GetTypes()
                .Where(type => type.IsVisible)
                .OrderBy(NormalizeType, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < types.Length; i++)
            {
                AddTypeSignatures(types[i], signatures);
            }

            signatures.Sort(StringComparer.Ordinal);
            return signatures.AsReadOnly();
        }

        internal static string ComputeSignatureDigest(
            IReadOnlyList<string> signatures)
        {
            if (signatures == null)
            {
                throw new ArgumentNullException(nameof(signatures));
            }

            string canonical = string.Join("\n", signatures) + "\n";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(canonical));
                return ToLowerHex(digest);
            }
        }

        internal static string DescribeDifference(
            IReadOnlyList<string> expected,
            IReadOnlyList<string> actual)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            if (actual == null)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            HashSet<string> actualSet = new HashSet<string>(
                actual,
                StringComparer.Ordinal);
            HashSet<string> expectedSet = new HashSet<string>(
                expected,
                StringComparer.Ordinal);
            string[] removed = expected
                .Where(signature => !actualSet.Contains(signature))
                .OrderBy(signature => signature, StringComparer.Ordinal)
                .ToArray();
            string[] added = actual
                .Where(signature => !expectedSet.Contains(signature))
                .OrderBy(signature => signature, StringComparer.Ordinal)
                .ToArray();

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < removed.Length; i++)
            {
                builder.Append("REMOVED ");
                builder.AppendLine(removed[i]);
            }

            for (int i = 0; i < added.Length; i++)
            {
                builder.Append("ADDED ");
                builder.AppendLine(added[i]);
            }

            return builder.ToString().TrimEnd('\r', '\n');
        }

        internal static FoldCanvasPublicApiManifestDocument Parse(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    "API manifest JSON must be non-empty.",
                    nameof(json));
            }

            FoldCanvasPublicApiManifestDocument document =
                JsonUtility.FromJson<FoldCanvasPublicApiManifestDocument>(json);
            if (document == null)
            {
                throw new FormatException("API manifest JSON is invalid.");
            }

            return document;
        }

        private static void AddTypeSignatures(
            Type type,
            ICollection<string> signatures)
        {
            signatures.Add(DescribeType(type));

            BindingFlags flags = BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly;

            ConstructorInfo[] constructors = type
                .GetConstructors(flags)
                .OrderBy(DescribeConstructor, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < constructors.Length; i++)
            {
                signatures.Add(DescribeConstructor(constructors[i]));
            }

            FieldInfo[] fields = type
                .GetFields(flags)
                .Where(field => !field.IsSpecialName)
                .OrderBy(DescribeField, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < fields.Length; i++)
            {
                signatures.Add(DescribeField(fields[i]));
            }

            PropertyInfo[] properties = type
                .GetProperties(flags)
                .Where(HasPublicAccessor)
                .OrderBy(DescribeProperty, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < properties.Length; i++)
            {
                signatures.Add(DescribeProperty(properties[i]));
            }

            EventInfo[] events = type
                .GetEvents(flags)
                .Where(HasPublicAccessor)
                .OrderBy(DescribeEvent, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < events.Length; i++)
            {
                signatures.Add(DescribeEvent(events[i]));
            }

            MethodInfo[] methods = type
                .GetMethods(flags)
                .Where(method => !IsPropertyOrEventAccessor(method))
                .OrderBy(DescribeMethod, StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < methods.Length; i++)
            {
                signatures.Add(DescribeMethod(methods[i]));
            }
        }

        private static string DescribeType(Type type)
        {
            StringBuilder builder = new StringBuilder("type public ");
            if (type.IsEnum)
            {
                builder.Append("enum ");
            }
            else if (type.IsInterface)
            {
                builder.Append("interface ");
            }
            else if (type.IsValueType)
            {
                builder.Append(type.IsByRefLike ? "ref-struct " : "struct ");
            }
            else if (type.IsAbstract && type.IsSealed)
            {
                builder.Append("static-class ");
            }
            else
            {
                if (type.IsAbstract)
                {
                    builder.Append("abstract-");
                }

                if (type.IsSealed)
                {
                    builder.Append("sealed-");
                }

                builder.Append("class ");
            }

            builder.Append(NormalizeType(type));
            if (type.IsEnum)
            {
                builder.Append(" : ");
                builder.Append(NormalizeType(Enum.GetUnderlyingType(type)));
                return builder.ToString();
            }

            Type baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object) && !type.IsValueType)
            {
                builder.Append(" : ");
                builder.Append(NormalizeType(baseType));
            }

            Type[] interfaces = type
                .GetInterfaces()
                .OrderBy(NormalizeType, StringComparer.Ordinal)
                .ToArray();
            if (interfaces.Length > 0)
            {
                builder.Append(" implements ");
                builder.Append(string.Join(",", interfaces.Select(NormalizeType)));
            }

            AppendGenericConstraints(builder, type.GetGenericArguments());
            return builder.ToString();
        }

        private static string DescribeConstructor(ConstructorInfo constructor)
        {
            return "ctor " + NormalizeType(constructor.DeclaringType) +
                "(" + DescribeParameters(constructor.GetParameters()) + ")";
        }

        private static string DescribeField(FieldInfo field)
        {
            StringBuilder builder = new StringBuilder("field ");
            if (field.IsStatic)
            {
                builder.Append("static ");
            }

            if (field.IsLiteral)
            {
                builder.Append("const ");
            }
            else if (field.IsInitOnly)
            {
                builder.Append("readonly ");
            }

            builder.Append(NormalizeType(field.FieldType));
            builder.Append(' ');
            builder.Append(NormalizeType(field.DeclaringType));
            builder.Append('.');
            builder.Append(field.Name);
            if (field.IsLiteral)
            {
                builder.Append(" = ");
                builder.Append(FormatConstant(field.GetRawConstantValue()));
            }

            return builder.ToString();
        }

        private static string DescribeProperty(PropertyInfo property)
        {
            StringBuilder builder = new StringBuilder("property ");
            MethodInfo accessor = property.GetMethod ?? property.SetMethod;
            if (accessor != null && accessor.IsStatic)
            {
                builder.Append("static ");
            }

            builder.Append(NormalizeType(property.PropertyType));
            builder.Append(' ');
            builder.Append(NormalizeType(property.DeclaringType));
            builder.Append('.');
            builder.Append(property.Name);
            ParameterInfo[] indices = property.GetIndexParameters();
            if (indices.Length > 0)
            {
                builder.Append('[');
                builder.Append(DescribeParameters(indices));
                builder.Append(']');
            }

            builder.Append(" { ");
            if (property.GetMethod != null && property.GetMethod.IsPublic)
            {
                builder.Append("get; ");
            }

            if (property.SetMethod != null && property.SetMethod.IsPublic)
            {
                builder.Append("set; ");
            }

            builder.Append('}');
            return builder.ToString();
        }

        private static string DescribeEvent(EventInfo eventInfo)
        {
            MethodInfo accessor = eventInfo.AddMethod ?? eventInfo.RemoveMethod;
            string prefix = accessor != null && accessor.IsStatic
                ? "event static "
                : "event ";
            return prefix + NormalizeType(eventInfo.EventHandlerType) + " " +
                NormalizeType(eventInfo.DeclaringType) + "." + eventInfo.Name;
        }

        private static string DescribeMethod(MethodInfo method)
        {
            StringBuilder builder = new StringBuilder("method ");
            if (method.IsStatic)
            {
                builder.Append("static ");
            }
            else if (method.IsAbstract)
            {
                builder.Append("abstract ");
            }
            else if (method.IsVirtual && method.GetBaseDefinition() == method)
            {
                builder.Append("virtual ");
            }
            else if (method.IsVirtual)
            {
                builder.Append("override ");
            }

            builder.Append(NormalizeType(method.ReturnType));
            builder.Append(' ');
            builder.Append(NormalizeType(method.DeclaringType));
            builder.Append('.');
            builder.Append(method.Name);
            Type[] genericArguments = method.IsGenericMethodDefinition
                ? method.GetGenericArguments()
                : Array.Empty<Type>();
            if (genericArguments.Length > 0)
            {
                builder.Append('<');
                builder.Append(string.Join(",", genericArguments.Select(
                    argument => argument.Name)));
                builder.Append('>');
            }

            builder.Append('(');
            builder.Append(DescribeParameters(method.GetParameters()));
            builder.Append(')');
            AppendGenericConstraints(builder, genericArguments);
            return builder.ToString();
        }

        private static string DescribeParameters(ParameterInfo[] parameters)
        {
            return string.Join(
                ",",
                parameters.Select(DescribeParameter));
        }

        private static string DescribeParameter(ParameterInfo parameter)
        {
            StringBuilder builder = new StringBuilder();
            if (parameter.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                builder.Append("params ");
            }

            if (parameter.IsOut)
            {
                builder.Append("out ");
            }
            else if (parameter.ParameterType.IsByRef)
            {
                builder.Append(parameter.IsIn ? "in " : "ref ");
            }

            builder.Append(NormalizeType(parameter.ParameterType));
            if (parameter.IsOptional)
            {
                builder.Append(" = ");
                builder.Append(FormatConstant(parameter.DefaultValue));
            }

            return builder.ToString();
        }

        private static void AppendGenericConstraints(
            StringBuilder builder,
            IEnumerable<Type> genericArguments)
        {
            foreach (Type argument in genericArguments)
            {
                if (!argument.IsGenericParameter)
                {
                    continue;
                }

                List<string> constraints = new List<string>();
                GenericParameterAttributes attributes =
                    argument.GenericParameterAttributes;
                GenericParameterAttributes special = attributes &
                    GenericParameterAttributes.SpecialConstraintMask;
                if ((special & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                {
                    constraints.Add("class");
                }

                if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                {
                    constraints.Add("struct");
                }

                Type[] typeConstraints = argument
                    .GetGenericParameterConstraints()
                    .OrderBy(NormalizeType, StringComparer.Ordinal)
                    .ToArray();
                constraints.AddRange(typeConstraints.Select(NormalizeType));
                if ((special & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                    (special & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                {
                    constraints.Add("new()");
                }

                if (constraints.Count > 0)
                {
                    builder.Append(" where ");
                    builder.Append(argument.Name);
                    builder.Append(':');
                    builder.Append(string.Join(",", constraints));
                }
            }
        }

        private static string NormalizeType(Type type)
        {
            if (type == null)
            {
                return "<null>";
            }

            if (type.IsByRef)
            {
                return NormalizeType(type.GetElementType()) + "&";
            }

            if (type.IsPointer)
            {
                return NormalizeType(type.GetElementType()) + "*";
            }

            if (type.IsArray)
            {
                return NormalizeType(type.GetElementType()) + "[" +
                    new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (type.IsGenericParameter)
            {
                return "!" + type.Name;
            }

            if (type.IsGenericType)
            {
                string name = type.GetGenericTypeDefinition().FullName ?? type.Name;
                return RemoveGenericArities(name) + "<" + string.Join(
                    ",",
                    type.GetGenericArguments().Select(NormalizeType)) + ">";
            }

            return type.FullName ?? type.Name;
        }

        private static string RemoveGenericArities(string name)
        {
            StringBuilder builder = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] != '`')
                {
                    builder.Append(name[i]);
                    continue;
                }

                while (i + 1 < name.Length && char.IsDigit(name[i + 1]))
                {
                    i++;
                }
            }

            return builder.ToString();
        }

        private static bool HasPublicAccessor(PropertyInfo property)
        {
            return (property.GetMethod != null && property.GetMethod.IsPublic) ||
                (property.SetMethod != null && property.SetMethod.IsPublic);
        }

        private static bool HasPublicAccessor(EventInfo eventInfo)
        {
            return (eventInfo.AddMethod != null && eventInfo.AddMethod.IsPublic) ||
                (eventInfo.RemoveMethod != null && eventInfo.RemoveMethod.IsPublic);
        }

        private static bool IsPropertyOrEventAccessor(MethodInfo method)
        {
            if (!method.IsSpecialName)
            {
                return false;
            }

            return method.Name.StartsWith("get_", StringComparison.Ordinal) ||
                method.Name.StartsWith("set_", StringComparison.Ordinal) ||
                method.Name.StartsWith("add_", StringComparison.Ordinal) ||
                method.Name.StartsWith("remove_", StringComparison.Ordinal);
        }

        private static string FormatConstant(object value)
        {
            if (value == null || value == DBNull.Value || value == Missing.Value)
            {
                return "null";
            }

            if (value is string text)
            {
                return "\"" + EscapeJson(text) + "\"";
            }

            if (value is char character)
            {
                return "'" + character.ToString() + "'";
            }

            if (value is bool boolean)
            {
                return boolean ? "true" : "false";
            }

            if (value is float single)
            {
                return single.ToString("R", CultureInfo.InvariantCulture);
            }

            if (value is double precision)
            {
                return precision.ToString("R", CultureInfo.InvariantCulture);
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private static void AppendProperty(
            StringBuilder builder,
            string name,
            string value,
            bool comma)
        {
            builder.Append("  \"");
            builder.Append(name);
            builder.Append("\": \"");
            builder.Append(EscapeJson(value));
            builder.Append(comma ? "\",\n" : "\"\n");
        }

        private static string EscapeJson(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            return builder.ToString();
        }

        private static string ToLowerHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }
}
