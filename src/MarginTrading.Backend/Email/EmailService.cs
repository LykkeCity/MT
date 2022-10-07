// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lykke.Service.EmailSender;
using Lykke.Service.EmailSender.AutorestClient.Models;
using MarginTrading.Backend.Core;

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
            var message =
                _templateGenerator.Generate("MarginCall", new {BaseAssetId = baseAssetId, AccountId = accountId});
            
            await _emailSender.SendAsync(
                new EmailMessage
                {
                    HtmlBody = message,
                    Subject = "Margin call"
                },
                new EmailAddressee
                {
                    EmailAddress = email
                });
        }

        public async Task SendStopOutEmailAsync(string email, string baseAssetId, string accountId)
        {
            var message =
                _templateGenerator.Generate("StopOut", new {BaseAssetId = baseAssetId, AccountId = accountId});

            await _emailSender.SendAsync(
                new EmailMessage
                {
                    HtmlBody = message,
                    Subject = "Stop out"
                },
                new EmailAddressee
                {
                    EmailAddress = email
                });
        }
    }
}
