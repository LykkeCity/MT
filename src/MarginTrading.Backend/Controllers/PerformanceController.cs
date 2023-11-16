// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using MarginTrading.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Route("api/performance")]
    [ApiController]
    public class PerformanceController : ControllerBase
    {
        /// <summary>
        /// Create statistics report for performance and return it as text file
        /// </summary>
        /// <returns></returns>
        [HttpGet("report")]
        public Task<IActionResult> GetStatisticsReport()
        {
            var txtReport = PerformanceLogger.PrintPerformanceStatistics();
            var fileContent = File(Encoding.UTF8.GetBytes(txtReport), "text/plain", "performance.txt");
            return Task.FromResult((IActionResult)fileContent);
        }
        
        /// <summary>
        /// Resets statistics report for performance
        /// </summary>
        /// <returns></returns>
        [HttpPost("report")]
        public Task<IActionResult> ResetStatisticsReport()
        {
            PerformanceTracker.Statistics.Clear();
            return Task.FromResult((IActionResult)Ok());
        }
    }
}