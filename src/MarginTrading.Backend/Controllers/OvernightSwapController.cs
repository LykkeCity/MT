using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
	[Authorize]
	[Route("api/overnightswap")]
	public class OvernightSwapController : Controller
	{
		public OvernightSwapController()
		{
			
		}

		/// <summary>
		/// Invoke recalculation of account/instrument/direction order packages that were not calculated successfully last time.
		/// </summary>
		/// <returns></returns>
		[Route("recalc.failed.orders")]
		[ProducesResponseType(200)]
		[ProducesResponseType(400)]
		[HttpPost]
		public async Task<IActionResult> RecalculateFailedOrders()
		{
			await MtServiceLocator.OvernightSwapService.CalculateAndChargeSwaps();

			return Ok();
		}
	}
}