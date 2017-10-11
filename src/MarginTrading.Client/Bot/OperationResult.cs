using System;

namespace MarginTrading.Client.Bot
{
    public class OperationResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Operation { get; set; }
        public object Result { get; set; }

        public TimeSpan Duration => EndDate - StartDate;
    }
}
