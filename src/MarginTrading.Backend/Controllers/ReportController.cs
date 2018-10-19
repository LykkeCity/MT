using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services;
using MarginTrading.Common.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/reports")]
    public class ReportController : Controller, IReportApi
    {
        private readonly IReportService _reportService;

        public ReportController(
            IReportService reportService)
        {
            _reportService = reportService;
        }
        
        /// <summary>
        /// Populates the data needed for report building to the storage: open positions and account fpl
        /// </summary>
        /// <returns>Returns 200 on success, exception otherwise</returns>
        [HttpPost("dump-data")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        public async Task DumpReportData()
        {
            await _reportService.DumpReportData();
        }
    }
}