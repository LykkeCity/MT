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