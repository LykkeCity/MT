using Microsoft.AspNetCore.Builder;

namespace MarginTrading.Frontend.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseOptions(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OptionsRequestsMiddleware>();
        }
        
        public static IApplicationBuilder UseGlobalErrorHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalErrorHandlerMiddleware>();
        }
    }
}