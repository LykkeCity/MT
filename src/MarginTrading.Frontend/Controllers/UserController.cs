using System.Threading.Tasks;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Settings;
using MarginTrading.Frontend.Extensions;
using MarginTrading.Frontend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [Route("api/user")]
    [Authorize]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class UserController: Controller
    {
        private readonly IClientAccountService _clientNotificationService;

        public UserController(IClientAccountService clientNotificationService)
        {
            _clientNotificationService = clientNotificationService;
        }

        [Route("notificationId")]
        [HttpGet]
        public async Task<ResponseModel<string>> GetNotificationId()
        {
            var clientId = this.GetClientId();

            if (clientId == null)
            {
                return this.UserNotFoundError<string>();
            }

            var notificationId = await _clientNotificationService.GetNotificationId(clientId);

            return ResponseModel<string>.CreateOk(notificationId);
        }
    }
}
