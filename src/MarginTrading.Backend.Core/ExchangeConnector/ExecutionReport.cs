// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core.ExchangeConnector
{
    public class ExecutionReport
    {
        /// <summary>
        /// Initializes a new instance of the ExecutionReport class.
        /// </summary>
        public ExecutionReport()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ExecutionReport class.
        /// </summary>
        /// <param name="type">A trade direction. Possible values include:
        /// 'Unknown', 'Buy', 'Sell'</param>
        /// <param name="time">Transaction time</param>
        /// <param name="price">An actual price of the execution or
        /// order</param>
        /// <param name="volume">Trade volume</param>
        /// <param name="fee">Execution fee</param>
        /// <param name="success">Indicates that operation was
        /// successful</param>
        /// <param name="executionStatus">Current status of the order. Possible
        /// values include: 'Unknown', 'Fill', 'PartialFill', 'Cancelled',
        /// 'Rejected', 'New', 'Pending'</param>
        /// <param name="failureType">Possible values include: 'None',
        /// 'Unknown', 'ExchangeError', 'ConnectorError',
        /// 'InsufficientFunds'</param>
        /// <param name="orderType">A type of the order. Possible values
        /// include: 'Unknown', 'Market', 'Limit'</param>
        /// <param name="execType">A type of the execution. ExecType = Trade
        /// means it is an execution, otherwise it is an order. Possible values
        /// include: 'Unknown', 'New', 'PartialFill', 'Fill', 'DoneForDay',
        /// 'Cancelled', 'Replace', 'PendingCancel', 'Stopped', 'Rejected',
        /// 'Suspended', 'PendingNew', 'Calculated', 'Expired', 'Restarted',
        /// 'PendingReplace', 'Trade', 'TradeCorrect', 'TradeCancel',
        /// 'OrderStatus'</param>
        /// <param name="clientOrderId">A client assigned ID of the
        /// order</param>
        /// <param name="exchangeOrderId">An exchange assigned ID of the
        /// order</param>
        /// <param name="instrument">An instrument description</param>
        /// <param name="feeCurrency">Fee currency</param>
        /// <param name="message">An arbitrary message from the exchange
        /// related to the execution|order</param>
        public ExecutionReport(
            TradeType type,
            DateTime time,
            double price,
            double volume,
            double fee,
            bool success,
            OrderExecutionStatus executionStatus,
            OrderStatusUpdateFailureType failureType,
            OrderType orderType,
            ExecType execType,
            string clientOrderId = null,
            string exchangeOrderId = null,
            Instrument instrument = null,
            string feeCurrency = null,
            string message = null)
        {
            this.ClientOrderId = clientOrderId;
            this.ExchangeOrderId = exchangeOrderId;
            this.Instrument = instrument;
            this.Type = type;
            this.Time = time;
            this.Price = price;
            this.Volume = volume;
            this.Fee = fee;
            this.FeeCurrency = feeCurrency;
            this.Success = success;
            this.ExecutionStatus = executionStatus;
            this.FailureType = failureType;
            this.Message = message;
            this.OrderType = orderType;
            this.ExecType = execType;
        }

        /// <summary>Gets a client assigned ID of the order</summary>
        [JsonProperty(PropertyName = "clientOrderId")]
        public string ClientOrderId { get; private set; }

        /// <summary>Gets an exchange assigned ID of the order</summary>
        [JsonProperty(PropertyName = "exchangeOrderId")]
        public string ExchangeOrderId { get; private set; }

        /// <summary>Gets an instrument description</summary>
        [JsonProperty(PropertyName = "instrument")]
        public Instrument Instrument { get; private set; }

        /// <summary>
        /// Gets a trade direction. Possible values include: 'Unknown', 'Buy',
        /// 'Sell'
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public TradeType Type { get; private set; }

        /// <summary>Gets transaction time</summary>
        [JsonProperty(PropertyName = "time")]
        public DateTime Time { get; private set; }

        /// <summary>Gets an actual price of the execution or order</summary>
        [JsonProperty(PropertyName = "price")]
        public double Price { get; private set; }

        /// <summary>Gets trade volume</summary>
        [JsonProperty(PropertyName = "volume")]
        public double Volume { get; private set; }

        /// <summary>Gets execution fee</summary>
        [JsonProperty(PropertyName = "fee")]
        public double Fee { get; private set; }

        /// <summary>Gets fee currency</summary>
        [JsonProperty(PropertyName = "feeCurrency")]
        public string FeeCurrency { get; private set; }

        /// <summary>Gets indicates that operation was successful</summary>
        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets current status of the order. Possible values include:
        /// 'Unknown', 'Fill', 'PartialFill', 'Cancelled', 'Rejected', 'New',
        /// 'Pending'
        /// </summary>
        [JsonProperty(PropertyName = "executionStatus")]
        public OrderExecutionStatus ExecutionStatus { get; set; }

        /// <summary>
        /// Gets possible values include: 'None', 'Unknown', 'ExchangeError',
        /// 'ConnectorError', 'InsufficientFunds'
        /// </summary>
        [JsonProperty(PropertyName = "failureType")]
        public OrderStatusUpdateFailureType FailureType { get; set; }

        /// <summary>
        /// Gets an arbitrary message from the exchange related to the
        /// execution|order
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; private set; }

        /// <summary>
        /// Gets a type of the order. Possible values include: 'Unknown',
        /// 'Market', 'Limit'
        /// </summary>
        [JsonProperty(PropertyName = "orderType")]
        public OrderType OrderType { get; private set; }

        /// <summary>
        /// Gets a type of the execution. ExecType = Trade means it is an
        /// execution, otherwise it is an order. Possible values include:
        /// 'Unknown', 'New', 'PartialFill', 'Fill', 'DoneForDay', 'Cancelled',
        /// 'Replace', 'PendingCancel', 'Stopped', 'Rejected', 'Suspended',
        /// 'PendingNew', 'Calculated', 'Expired', 'Restarted',
        /// 'PendingReplace', 'Trade', 'TradeCorrect', 'TradeCancel',
        /// 'OrderStatus'
        /// </summary>
        [JsonProperty(PropertyName = "execType")]
        public ExecType ExecType { get; private set; }

        /// <summary>Validate the object.</summary>
        /// <exception cref="T:Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
        }
    }
}