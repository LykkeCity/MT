using MarginTrading.ExternalOrderBroker.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.ExternalOrderBroker.Repositories.Azure
{
    internal class ExternalOrderReportEntity : TableEntity, IExternalOrderReport
    {

        public string Id
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }
        
        public string Exchange
        {
            get => RowKey;
            set => RowKey = value;
        }
        
        public string Instrument { get; set; }
        
        public string BaseAsset { get; set; }
        
        public string QuoteAsset { get; set; }

        public string Type { get; set; }

        public System.DateTime Time { get; set; }

        public double Price { get; set; }

        public double Volume { get; set; }

        public double Fee { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }

        public static ExternalOrderReportEntity Create(IExternalOrderReport externalContract)
        {
            return new ExternalOrderReportEntity
            {
                Instrument = externalContract.Instrument,
                Exchange = externalContract.Exchange,
                BaseAsset = externalContract.BaseAsset,
                QuoteAsset = externalContract.QuoteAsset,
                Type = externalContract.Type,
                Time = externalContract.Time,
                Price = externalContract.Price,
                Volume = externalContract.Volume * 
                         (externalContract.Type == TradeTypeReport.Buy.ToString() ? 1 : -1),
                Fee = externalContract.Fee,
                Id = externalContract.Id,
                Status = externalContract.Status,
                Message = externalContract.Message
            };
        }
    }
}