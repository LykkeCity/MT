using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Snow.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers.Snow
{
    /// <summary>
    /// Provides data about active orders
    /// </summary>
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : Controller, IOrdersApi
    {
        /// <summary>
        /// Get order by id 
        /// </summary>
        [HttpGet, Route("{orderId}")]
        public Task<OrderContract> Get(string orderId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get orders by parent order id
        /// </summary>
        [HttpGet, Route("by-parent/{parentOrderId}")]
        public Task<List<OrderContract>> ListByParent(string parentOrderId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [HttpGet, Route("open")]
        public Task<List<OrderContract>> ListOpen(string accountId, string instrument)
        {
            throw new System.NotImplementedException();
        }

        //todo: move to history
        /// <summary>
        /// Get executed orders with optional filtering
        /// </summary>
        [HttpGet, Route("executed")]
        public Task<List<OrderContract>> ListExecuted(string accountId, string instrument)
        {
            throw new System.NotImplementedException();
        }
    }
}