using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Contract.BackendContracts;
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

        [UsedImplicitly]
        public async Task Invoke(HttpContext context)
        {
            var isMaintenanceMode = false;
            try
            {
                isMaintenanceMode = !context.Request.Path.ToString().StartsWith("/swagger") &&
                                    context.Request.Path != $"/api/service/{LykkeConstants.MaintenanceModeRoute}" &&
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
                    var response = new MtBackendResponse<string> { ErrorMessage = "Maintenance Mode" };
                    await context.Response.WriteAsync(response.ToJson());
                }
            }
        }
    }
}
