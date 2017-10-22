using System;
using System.Collections.Generic;
using System.Text;
using Common;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    internal static class Trace
    {
        public static void Write(string str)
        {
            //if (str.Length > 140)
            //{
            //    str = str.Substring(0, 140);
            //}

            Console.WriteLine(str);
        }

        public static void Write(object obj)
        {
            Write(obj.ToJson());
        }

        public static void Write(string str, object obj)
        {
            Write(str + ": " + obj.ToJson());
        }
    }
}
