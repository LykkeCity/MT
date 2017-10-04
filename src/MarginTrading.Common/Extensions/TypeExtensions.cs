using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MarginTrading.Common.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, string> TypeAliases = new Dictionary<Type, string>
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" },
            { typeof(byte?), "byte?" },
            { typeof(sbyte?), "sbyte?" },
            { typeof(short?), "short?" },
            { typeof(ushort?), "ushort?" },
            { typeof(int?), "int?" },
            { typeof(uint?), "uint?" },
            { typeof(long?), "long?" },
            { typeof(ulong?), "ulong?" },
            { typeof(float?), "float?" },
            { typeof(decimal?), "decimal?" },
            { typeof(double?), "double?" },
            { typeof(bool?), "bool?" },
            { typeof(char?), "char?" },
            { typeof(DateTime?), "DateTime?" },
            { typeof(string[]), "string[]" }
        };

        public static string GetTypeDefinition(this Type type)
        {
            var sb = new StringBuilder();
            var isEnum = type.GetTypeInfo().IsEnum;
            sb.AppendLine($"public {(isEnum ? "enum": "class")} {type.GetTypeName()}");
            sb.AppendLine("{");

            if (isEnum)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public|BindingFlags.Static);

                for (var i = 0; i < fields.Length; i++)
                {
                    sb.AppendLine($"   {fields[i].Name} = {i}");
                }
            }
            else
            {
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                foreach (PropertyInfo property in properties)
                {
                    sb.AppendLine(property.PropertyType.IsDictionary() ? GetDictionaryProperty(property) : GetProperty(property));
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string GetPropertyTypeAlias(this Type type)
        {
            return TypeAliases.ContainsKey(type) ? TypeAliases[type] : type.GetTypeName();
        }

        public static string GetTypeName(this Type type)
        {
            return !type.GetTypeInfo().IsGenericType 
                ? type.Name 
                : type.Name.Split('`')[0];
        }

        public static bool IsUserDefinedClass(this Type type)
        {
            return !type.Namespace.StartsWith("System");
        }

        public static bool IsDictionary(this Type type)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        private static string GetProperty(PropertyInfo property)
        {
            return $"   public {GetPropertyTypeAlias(property.PropertyType)} {property.Name} {{ get; set; }}";
        }

        private static string GetDictionaryProperty(PropertyInfo property)
        {
            return $"   public Dictionary<{GetPropertyTypeAlias(property.PropertyType.GenericTypeArguments[0])}, {GetPropertyTypeAlias(property.PropertyType.GenericTypeArguments[1])}> {property.Name} {{ get; set; }}";
        }
    }
}
