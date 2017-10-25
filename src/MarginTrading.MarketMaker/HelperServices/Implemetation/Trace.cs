using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    internal static class Trace
    {
        private static readonly BlockingCollection<string> _consoleQueue = new BlockingCollection<string>(10000);
        private static readonly ConcurrentStack<string> _lastElemsStack = new ConcurrentStack<string>();

        static Trace()
        {
            Task.Run(() =>
            {
                while (true)
                    foreach (var str in _consoleQueue.GetConsumingEnumerable())
                        Console.WriteLine(str);
            });
        }

        public static void Write(string str)
        {
            _lastElemsStack.Push(str);
            if (_lastElemsStack.Count > 500)
                _lastElemsStack.TryPop(out var _);

            _consoleQueue.Add(str);
        }

        public static void Write(object obj)
        {
            Write(obj.ToJson());
        }

        public static void Write(string str, object obj)
        {
            Write(str + ": " + obj.ToJson());
        }

        public static IReadOnlyList<string> GetLast()
        {
            return _lastElemsStack.ToArray();
        }
    }
}
