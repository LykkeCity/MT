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
            operation.OperationId = actionDescriptor.ActionName;
        }
    }
}
