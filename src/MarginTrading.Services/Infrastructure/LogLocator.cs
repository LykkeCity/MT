using Common.Log;

namespace MarginTrading.Services.Infrastructure
{
    public static class LogLocator
    {
        public static ILog CommonLog { get; set; }
        public static ILog RequestsLog { get; set; }
    }
}
