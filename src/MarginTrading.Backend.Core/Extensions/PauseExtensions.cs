// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Rfq;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class PauseExtensions
    {
        public static object ToParameters(this Pause pause)
        {
            return new
            {
                pause.OperationId,
                pause.OperationName,
                Source = pause.Source.ToString(),
                pause.CreatedAt,
                State = pause.State.ToString(),
                Initiator = pause.Initiator.ToString()
            };
        }
    }
}