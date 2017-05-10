using System.Threading.Tasks;
using Lykke.EmailSenderProducer.Interfaces;
using Lykke.EmailSenderProducer.Models;
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
            var emailMessage = new EmailMessage
            {
                Body = message,
                IsHtml = true,
                Subject = "MarginCall"
            };

            await _emailSender.SendEmailAsync(email, emailMessage);
        }
    }

}
