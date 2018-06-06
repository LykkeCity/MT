using Lykke.Service.EmailSender;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services.Client;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace MarginTrading.Backend.Email
{
    public class EmailService : IEmailService
    {
        private readonly ITemplateGenerator _templateGenerator;
        private readonly IEmailSender _emailSender;
        private readonly IClientAccountService _clientAccountService;

        public EmailService(ITemplateGenerator templateGenerator, IEmailSender emailSender, IClientAccountService clientAccountService)
        {
            _templateGenerator = templateGenerator;
            _emailSender = emailSender;
            _clientAccountService = clientAccountService;
        }

        public async Task SendMarginCallEmailAsync(IMarginTradingAccount account)
        {
            var clientEmail = await _clientAccountService.GetEmail(account.ClientId);
            if (string.IsNullOrEmpty(clientEmail))
                return;
            
            var message =
                _templateGenerator.Generate(GetMarginCallTemplate(account.LegalEntity), new { account.BaseAssetId, AccountId = account.Id, System.DateTime.UtcNow.Year });

            await _emailSender.SendAsync(
                new EmailMessage
                {
                    HtmlBody = message,
                    Subject = "Margin call"
                },
                new EmailAddressee
                {
                    EmailAddress = clientEmail
                });
        }
                
        public async Task SendStopOutEmailAsync(IMarginTradingAccount account)
        {
            var clientEmail = await _clientAccountService.GetEmail(account.ClientId);
            if (string.IsNullOrEmpty(clientEmail))
                return;

            var message =
                _templateGenerator.Generate(GetStopOutTemplate(account.LegalEntity), new { account.BaseAssetId, AccountId = account.Id, System.DateTime.UtcNow.Year });

            await _emailSender.SendAsync(
                new EmailMessage
                {
                    HtmlBody = message,
                    Subject = "Stop out"
                },
                new EmailAddressee
                {
                    EmailAddress = clientEmail
                });
        }

        public async Task SendOvernightSwapEmailAsync(string email, OvernightSwapNotification overnightSwapNotification)
        {
            var message =
                _templateGenerator.Generate("OvernightSwap", overnightSwapNotification);

            await _emailSender.SendAsync(
                new EmailMessage
                {
                    HtmlBody = message,
                    Subject = "Overnight Swap"
                },
                new EmailAddressee
                {
                    EmailAddress = email
                });
        }


        private string GetMarginCallTemplate(string legalEntity)
        {
            string result = "MarginCall";
            if (legalEntity == LykkeConstants.LykkeCyprusLegalEntity)
                result = "MarginCall.cyprus";
            return result;
        }
        private string GetStopOutTemplate(string legalEntity)
        {
            string result = "StopOut";
            if (legalEntity == LykkeConstants.LykkeCyprusLegalEntity)
                result = "StopOut.cyprus";
            return result;
        }
    }
}
