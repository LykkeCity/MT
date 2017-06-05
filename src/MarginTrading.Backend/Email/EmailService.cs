using System.Threading.Tasks;
using Lykke.Service.EmailSender;
using MarginTrading.Core;

#pragma warning disable 1591

namespace MarginTrading.Backend.Email
{
    public class EmailService : IEmailService
    {
        private readonly ITemplateGenerator _templateGenerator;
        private readonly IEmailSender _emailSender;

        public EmailService(ITemplateGenerator templateGenerator, IEmailSender emailSender)
        {
            _templateGenerator = templateGenerator;
            _emailSender = emailSender;
        }

        public async Task SendMarginCallEmailAsync(string email, string baseAssetId, string accountId)
        {
            string message = _templateGenerator.Generate("MarginCall", new { BaseAssetId = baseAssetId, AccountId = accountId });

            await _emailSender.SendAsync(new EmailMessage
            {
                HtmlBody = message,
                Subject = "Margin call",
                ToEmailAddress = email
            });
        }
    }

}
