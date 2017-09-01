using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.Swagger.Model;
using Swashbuckle.SwaggerGen.Generator;

namespace MarginTrading.DataReader.Infrastructure
{
    public class CustomOperationIdOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var actionDescriptor = (ControllerActionDescriptor)context.ApiDescription.ActionDescriptor;
            var method = ConvertToSentenceCase(context.ApiDescription.HttpMethod);
            operation.OperationId = $"{method}{actionDescriptor.ControllerName}{actionDescriptor.ActionName}";
        }

        private static string ConvertToSentenceCase(string str)
        {
            return str[0].ToString().ToUpper() + str.Substring(1).ToLower();
        }
    }
}
