using System;
using FluentScheduler;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services;

namespace MarginTrading.Backend.Scheduling
{
	/// <summary>
	/// Overnight swaps calculation job.
	/// Take into account, that scheduler might fire the job with delay of 100ms.
	/// </summary>
	public class OvernightSwapJob : IJob, IDisposable
	{

		public OvernightSwapJob()
		{
		}
		
		public void Execute()
		{
			MtServiceLocator.OvernightSwapService.CalculateAndChargeSwaps();
		}

		public void Dispose()
		{
		}
	}
}