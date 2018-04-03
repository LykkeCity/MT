using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
	public interface IOvernightSwapHistory : IOvernightSwapState
	{
		bool IsSuccess { get; }
		Exception Exception { get; }
	}
}