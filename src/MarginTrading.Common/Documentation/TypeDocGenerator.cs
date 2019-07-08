// Copyright (c) 2019 Lykke Corp.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Common.Documentation
{
    public class TypeDocGenerator
    {
        public MethodDocInfo[] GetDocumentation(Type type)
        {
            var result = new List<MethodDocInfo>();
            var methods = GetAvailableMethods(type);

            foreach (var method in methods)
            {
                var attr = (DocMeAttribute)method.GetCustomAttribute(typeof(DocMeAttribute));
                var returnType = method.ReturnType.IsConstructedGenericType ? method.ReturnType.GenericTypeArguments[0] : method.ReturnType;
                var types = GetTypes(returnType);
                var input = GetInputParametersAsString(method);
                var inputTypes = GetTypes(attr.InputType);

                var docInfo = new MethodDocInfo
                {
                    Id = attr.Name.Replace(".", "_")
                             .Replace("{", "_")
                             .Replace("}", "_").ToLower() + "_Id",
                    Name = attr.Name,
                    Input = input,
                    Output = returnType.GetTypeName(),
                    Description = attr.Description,
                    InputTypes = inputTypes.ToArray(),
                    OutputTypes = types.ToArray()
                };

                result.Add(docInfo);
            }

            return result.ToArray();
        }

        private MethodInfo[] GetAvailableMethods(Type type)
        {
            return type.GetMethods().Where(item => item.CustomAttributes.Any(a => a.AttributeType == typeof(DocMeAttribute))).ToArray();
        }

        private string GetInputParametersAsString(MethodInfo methodInfo)
        {
            return string.Join(", ", methodInfo.GetParameters().Select(item => $"{item.ParameterType.GetPropertyTypeAlias()} {item.Name}")
                .ToArray());
        }

        private List<Type> GetTypes(Type type)
        {
            if (type == null)
                return new List<Type>();

            var elementType = type.GetElementType() ?? type;
            var properties = elementType.GetProperties();

            if (properties.Length == 0)
                return new List<Type> { type };

            var types = new List<Type> { elementType };

            foreach (var property in properties)
            {
                if (property.PropertyType.IsUserDefinedClass())
                {
                    types.AddRange(GetTypes(property.PropertyType));
                }

                if (property.PropertyType.IsDictionary())
                {
                    foreach (var dictType in property.PropertyType.GenericTypeArguments)
                    {
                        if (dictType.IsUserDefinedClass())
                        {
                            types.AddRange(GetTypes(dictType));
                        }
                    }
                }
            }

            return types.Distinct().ToList();
        }
    }
}
