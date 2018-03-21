using System;
using FluentScheduler;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services;

namespace MarginTrading.Backend.Scheduling
{
	public class OvernightSwapJob : IJob, IDisposable
	{

		public OvernightSwapJob()
		{
		}
		
		public void Execute()
		{
			MtServiceLocator.OvernightSwapService.CalculateAndChargeSwaps().GetAwaiter().GetResult();
		}

		public void Dispose()
		{
		}
	}
}