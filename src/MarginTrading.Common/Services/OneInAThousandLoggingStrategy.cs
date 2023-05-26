// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace MarginTrading.Common.Services
{
    public sealed class OneInAThousandLoggingStrategy : IRabbitMqPublisherLoggingStrategy
    {
        private int _counter = 0;
        private const int StepSize = 1000;

        public bool CanLog()
        {
            var result = Interlocked.Increment(ref _counter);
            if (result >= StepSize)
            {
                Interlocked.Exchange(ref _counter, 0);
            }

            return result % StepSize == 0;
        }
    }
}