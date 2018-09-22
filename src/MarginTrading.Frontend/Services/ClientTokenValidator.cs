using System.Collections.Generic;
using System.Security.Claims;
using MarginTrading.Frontend.Infrastructure;
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

            var ls = new List<Claim>();
            var clientId = _clientTokenService.GetClientId(securityToken).Result;
            ls.Add(new Claim(AuthConsts.SubjectClaim, clientId, ClaimValueTypes.String));
            var id = new ClaimsIdentity(ls, AuthConsts.LykkeBearerScheme);
            var principal = new ClaimsPrincipal(id);
            return principal;
        }

        public bool CanValidateToken => true;
        public int MaximumTokenSizeInBytes { get; set; }
    }
}
