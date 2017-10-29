using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Core;
using MarginTrading.Services.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMaintenanceModeService _maintenanceModeService;
        private readonly ILog _log;

        public MaintenanceModeMiddleware(RequestDelegate next, 
            IMaintenanceModeService maintenanceModeService,
            ILog log)
        {
            _next = next;
            _maintenanceModeService = maintenanceModeService;
            _log = log;
        }

        public async Task Invoke(HttpContext context)
        {
            bool isMaintenanceMode = false;
            try
            {
                isMaintenanceMode = !context.Request.Path.ToString().StartsWith("/swagger") &&
                                    context.Request.Path != $"/api/backoffice/{LykkeConstants.MaintenanceModeRoute}" &&
                                    _maintenanceModeService.CheckIsEnabled();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("MIDDLEWARE", "MaintenanceModeMiddleware", "", ex);
            }
            finally
            {
                if (!isMaintenanceMode)
                    await _next.Invoke(context);
                else
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 503;
                    var response = new MtBackendResponse<string> { Message = "Maintenance Mode" };
                    await context.Response.WriteAsync(response.ToJson());
                }
            }
        }
    }
}
