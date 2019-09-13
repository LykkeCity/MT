// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts.ExchangeConnector
{
    [PublicAPI]
    public interface IExchangeConnectorClient
    {
        [Post("/api/v1/Orders")]
        Task<ExecutionReport> ExecuteOrder(OrderModel orderModel,
            CancellationToken cancellationToken = default);
    }
}