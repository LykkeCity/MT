using Newtonsoft.Json;

namespace MarginTrading.ExternalOrderBroker.Models
{
	public partial class InstrumentReport
	{
		/// <summary>
		/// Initializes a new instance of the Instrument class.
		/// </summary>
		public InstrumentReport()
		{
			CustomInit();
		}

		/// <summary>
		/// Initializes a new instance of the Instrument class.
		/// </summary>
		public InstrumentReport(string name = default(string), string exchange = default(string),
			string baseProperty = default(string), string quote = default(string))
		{
			Name = name;
			Exchange = exchange;
			BaseProperty = baseProperty;
			Quote = quote;
			CustomInit();
		}

		/// <summary>
		/// An initialization method that performs custom operations like setting defaults
		/// </summary>
		partial void CustomInit();

		/// <summary>
		/// </summary>
		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		/// <summary>
		/// </summary>
		[JsonProperty(PropertyName = "exchange")]
		public string Exchange { get; set; }

		/// <summary>
		/// </summary>
		[JsonProperty(PropertyName = "base")]
		public string BaseProperty { get; set; }

		/// <summary>
		/// </summary>
		[JsonProperty(PropertyName = "quote")]
		public string Quote { get; set; }
	}
}