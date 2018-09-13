using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using MarginTrading.Frontend.Infrastructure;
using MarginTrading.Frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Extensions
{
    public static class ControllerExtensions
    {
        public static string GetClientId(this Controller controller)
        {
            return controller.User.GetClaim(AuthConsts.SubjectClaim);
        }

        public static ResponseModel<T> UserNotFoundError<T>(this Controller controller)
        {
            return ResponseModel<T>.CreateFail(ResponseModel.ErrorCodeType.NoAccess, "User not found");
        }
    }
}
