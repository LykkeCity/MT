using System;

namespace MarginTrading.Client
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(DateTime date, string origin, string type, string message, Exception exception)
        {
            Date = date;
            Origin = origin;
            Type = type;
            Message = message;
            Exception = exception;
        }

        public DateTime Date { get; }
        public string Origin { get; }
        public string Type { get; }
        public string Message { get; }
        public Exception Exception { get; }
    }
}
