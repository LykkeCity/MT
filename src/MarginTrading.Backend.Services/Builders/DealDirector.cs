// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Positions;

namespace MarginTrading.Backend.Services.Builders
{
    /// <summary>
    /// The orchestrator of the deal contract build process
    /// </summary>
    internal static class DealDirector
    {
        /// <summary>
        /// Creates an instance of <see cref="DealContract"/> using proper <see cref="DealBuilder"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static DealContract Construct(DealBuilder builder)
        {
            // the sequence of steps matters
            return builder.AddIdentity()
                .AddOpenPart()
                .AddClosePart()
                .AddPnl()
                .AddChargedPnl()
                .Build();
        }
    }
}