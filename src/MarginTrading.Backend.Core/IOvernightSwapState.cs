using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
	public interface IOvernightSwapState
	{
		string AccountId { get; }
		string Instrument { get; }
		OrderDirection? Direction { get; }
		DateTime Timestamp { get; }
		List<string> OpenOrderIds { get; }
		decimal Value { get; }
		decimal SwapRate { get; }
	}
}