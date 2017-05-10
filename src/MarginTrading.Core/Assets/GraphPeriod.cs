using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Core.Assets
{
    public sealed class GraphPeriod
    {
        public static readonly GraphPeriod Hour = new GraphPeriod("1H", "1 H");
        public static readonly GraphPeriod Day = new GraphPeriod("1D", "1 D");
        public static readonly GraphPeriod Day3 = new GraphPeriod("3D", "3 D");
        public static readonly GraphPeriod Month = new GraphPeriod("1M", "1 M");
        public static readonly GraphPeriod Year = new GraphPeriod("1Y", "1 Y");

        private GraphPeriod(string value, string name)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }

        public override string ToString()
        {
            return Name;
        }

        public static DateTime SubstractPeriod(DateTime dt, string periodVal)
        {
            switch (periodVal)
            {
                case "1H":
                    dt = dt.AddHours(-1);
                    break;
                case "1D":
                    dt = dt.AddDays(-1);
                    break;
                case "3D":
                    dt = dt.AddDays(-3);
                    break;
                case "1M":
                    dt = dt.AddMonths(-1);
                    break;
                case "1Y":
                    dt = dt.AddYears(-1);
                    break;
            }
            return dt;
        }

        public static IEnumerable<GraphPeriod> CurrentPeriods => new List<GraphPeriod>
        {
            Hour,
            Day,
            Day3,
            Month,
            Year
        };

        public static bool IsValidGraphPeriod(string periodVal)
        {
            return Enumerable.Any(CurrentPeriods, x => x.Value == periodVal);
        }

        public static GraphPeriod ByVal(string val)
        {
            return Enumerable.First(CurrentPeriods, x => x.Value == val);
        }
    }
}