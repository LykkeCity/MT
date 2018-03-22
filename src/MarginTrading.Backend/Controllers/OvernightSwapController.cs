using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
	[Authorize]
	[Route("api/overnightswap")]
	public class OvernightSwapController : Controller
	{
		private readonly IOvernightSwapHistoryRepository _overnightSwapHistoryRepository;
		
		public OvernightSwapController(IOvernightSwapHistoryRepository overnightSwapHistoryRepository)
		{
			_overnightSwapHistoryRepository = overnightSwapHistoryRepository;
		}
		
		[Route("history")]
		[ProducesResponseType(typeof(IEnumerable<IOvernightSwapHistory>), 200)]
		[ProducesResponseType(400)]
		[HttpPost]
		public async Task<IActionResult> GetOvernightSwapHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
		{
			var data = await _overnightSwapHistoryRepository.GetAsync(from, to);

			return Ok(data);
		}

		/// <summary>
		/// Invoke recalculation of account/instrument/direction order packages that were not calculated successfully last time.
		/// </summary>
		/// <returns></returns>
		[Route("recalc.failed.orders")]
		[ProducesResponseType(200)]
		[ProducesResponseType(400)]
		[HttpPost]
		public IActionResult RecalculateFailedOrders()
		{
			MtServiceLocator.OvernightSwapService.CalculateAndChargeSwaps();

			return Ok();
		}
	}
}