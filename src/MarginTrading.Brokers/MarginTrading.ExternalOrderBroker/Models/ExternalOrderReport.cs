namespace MarginTrading.ExternalOrderBroker.Models
{
	public class ExternalOrderReport : IExternalOrderReport
	{
		public string Instrument { get; set; }
		
		public string Exchange { get; set; }
        
		public string BaseAsset { get; set; }
        
		public string QuoteAsset { get; set; }

		public string Type { get; set; }

		public System.DateTime Time { get; set; }

		public double Price { get; set; }

		public double Volume { get; set; }

		public double Fee { get; set; }

		public string Id { get; set; }

		public string Status { get; set; }

		public string Message { get; set; }

		public override string ToString()
		{
			return "Exchange: " + Exchange + ", "
			       + "Instrument: " + Instrument + ", "
			       + "Type: " + this.Type + ", "
			       + "Price: " + this.Price + ", "
			       + "Volume: " + this.Volume + ", "
			       + "Fee: " + this.Fee + ", "
			       + "OrderId: " + this.Id + ", "
			       + "Status: " + this.Status + ", "
			       + "Message: " + this.Message;
		}
		
		public static ExternalOrderReport Create(Lykke.Service.ExchangeConnector.Client.Models.ExecutionReport externalContract)
		{
			return new ExternalOrderReport
			{
				Instrument = externalContract.Instrument.Name,
				Exchange = externalContract.Instrument.Exchange,
				BaseAsset = externalContract.Instrument.BaseProperty,
				QuoteAsset = externalContract.Instrument.Quote,
				Type = externalContract.Type.ToString(),
				Time = externalContract.Time,
				Price = externalContract.Price,
				Volume = externalContract.Volume * 
				         (externalContract.Type == Lykke.Service.ExchangeConnector.Client.Models.TradeType.Buy ? 1 : -1),
				Fee = externalContract.Fee,
				Id = externalContract.ExchangeOrderId,
				Status = externalContract.ExecutionStatus.ToString(),
				Message = externalContract.Message ?? ""
			};
		}
	}
}