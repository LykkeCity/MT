// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.EmailSender;

namespace MarginTrading.Backend.Services.Stubs
{
    public class EmailSenderLogStub : IEmailSender
    {
        private readonly ILog _log;

        public EmailSenderLogStub(ILog log)
        {
            _log = log;
        }
        
        public void Dispose()
        {
            
        }

        public Task SendAsync(EmailMessage message, EmailAddressee to)
        {
            return _log.WriteInfoAsync(nameof(EmailSenderLogStub), nameof(SendAsync), message?.ToJson(),
                $"Email was send to {to.EmailAddress}");
        }
    }
}