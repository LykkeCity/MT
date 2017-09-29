using System;
using MarginTrading.DataReader.Filters;

namespace MarginTrading.DataReader.Attributes
{
    /// <summary>
    /// Indicates that filter <see cref="MarginTradingEnabledFilter"/> should not check if current type of margin trading (live or demo) is enabled for clientId.
    /// Used for marking actions which accept a clientId but can be called even if particular type of trading is disabled for client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SkipMarginTradingEnabledCheckAttribute: Attribute
    {
    }
}
