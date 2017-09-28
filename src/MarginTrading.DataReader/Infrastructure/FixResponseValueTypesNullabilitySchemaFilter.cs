using System;
using System.Linq;
using Swashbuckle.Swagger.Model;
using Swashbuckle.SwaggerGen.Generator;

namespace MarginTrading.DataReader.Infrastructure
{
    public class FixResponseValueTypesNullabilitySchemaFilter : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaFilterContext context)
        {
            if (schema.Type != "object" || schema.Properties == null)
            {
                return;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var nonNulableValueTypedPropNames = context.SystemType.GetProperties()
                .Where(p => p.PropertyType.IsValueType && Nullable.GetUnderlyingType(p.PropertyType) == null)
                .Select(p => p.Name);

            schema.Required = schema.Properties.Keys.Intersect(nonNulableValueTypedPropNames, StringComparer.OrdinalIgnoreCase).ToList();

            if (schema.Required.Count == 0)
            {
                schema.Required = null;
            }
        }
    }
}
