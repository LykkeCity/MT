// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Running;
using BenchmarkScenarios;

namespace ExternalOrderbooksBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<ConcurrentDictionaryVsReadWriteLockedDictionary>();
            var summary = BenchmarkRunner.Run<ExternalOrderbookServicesBenchmark>();
        }
    }
}