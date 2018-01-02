using System.Threading.Tasks;
using MarginTrading.Frontend.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Frontend.Middleware
{
    public class OptionsRequestsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CorsSettings _settings;

        public OptionsRequestsMiddleware(RequestDelegate next, CorsSettings settings)
        {
            _next = next;
            _settings = settings;
        }

        public async Task Invoke(HttpContext context)
        {
            if (_settings.HandleOptionsRequest && context.Request.Method == "OPTIONS")
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", _settings.AllowOrigins);
                context.Response.Headers.Add("Access-Control-Allow-Headers", _settings.AllowHeaders);
                context.Response.Headers.Add("Access-Control-Allow-Methods", _settings.AllowMethods);
                
                if (_settings.AllowCredentials)
                    context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("OK");
            }
            else
            {
                await this._next.Invoke(context);
            }
        }
    }

    public static class OptionsMiddlewareExtensions
    {
        public static IApplicationBuilder UseOptions(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OptionsRequestsMiddleware>();
        }
    }
}
