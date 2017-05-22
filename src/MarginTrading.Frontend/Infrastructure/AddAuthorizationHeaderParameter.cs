using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Swashbuckle.Swagger.Model;
using Swashbuckle.SwaggerGen.Generator;

namespace MarginTrading.Frontend.Infrastructure
{
    public class AddAuthorizationHeaderParameter : IOperationFilter
    {
        void IOperationFilter.Apply(Operation operation, OperationFilterContext context)
        {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            var isAuthorized = filterPipeline.Select(f => f.Filter).Any(f => f is AuthorizeFilter);
            var authorizationRequired = context.ApiDescription.GetControllerAttributes().Any(a => a is AuthorizeAttribute);
            if (!authorizationRequired) authorizationRequired = context.ApiDescription.GetActionAttributes().Any(a => a is AuthorizeAttribute);

            if (isAuthorized && authorizationRequired)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<IParameter>();

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "Authorization",
                    In = "header",
                    Description = "Bearer Token",
                    Required = true,
                    Type = "string"
                });
            }
        }
    }
}
