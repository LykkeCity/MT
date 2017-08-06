using Microsoft.AspNetCore.Builder;

namespace MarginTrading.Services.Middleware
{
    public class RequestLoggingPipeline
    {
        public void Configure(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<RequestsLoggingMiddleware>();
        }
    }
}
