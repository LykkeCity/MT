using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using MarginTrading.Backend.Core;
using Microsoft.IdentityModel.Tokens;

namespace MarginTrading.Frontend.Services
{
    public class ClientTokenValidator : ISecurityTokenValidator
    {
        private readonly IClientTokenService _clientTokenService;

        public ClientTokenValidator(IClientTokenService clientTokenService)
        {
            _clientTokenService = clientTokenService;
        }

        public bool CanReadToken(string securityToken) => true;

        public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            validatedToken = null;

            List<Claim> ls = new List<Claim>();
            var clientId = _clientTokenService.GetClientId(securityToken).Result;
            ls.Add(new Claim(ClaimTypes.NameIdentifier, clientId, ClaimValueTypes.String));
            ClaimsIdentity id = new ClaimsIdentity(ls, "magic");
            var principal = new ClaimsPrincipal(id);
            return principal;
        }

        public bool CanValidateToken => true;
        public int MaximumTokenSizeInBytes { get; set; }
    }
}
