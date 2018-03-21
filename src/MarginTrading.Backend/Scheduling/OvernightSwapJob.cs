using System;
using FluentScheduler;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Scheduling
{
	public class OvernightSwapJob : IJob, IDisposable
	{
		private readonly IOvernightSwapService _overnightSwapService;

		public OvernightSwapJob(IOvernightSwapService overnightSwapService)
		{
			_overnightSwapService = overnightSwapService;
		}
		
		public void Execute()
		{

			_overnightSwapService.CalculateSwaps();
		}

		public void Dispose()
		{
		}
	}
}