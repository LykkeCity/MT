using Common.Log;
using Lykke.Service.EmailSender;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Email;
using MarginTrading.Common.Services.Client;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MarginTradingTests
{
    [TestFixture]
    [Ignore("Local Test")]
    public class EmailSendingTests
    {

        IEmailService _emailService;        

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var mockEnvironment = new Mock<IHostingEnvironment>();
            

            mockEnvironment
                .Setup(m => m.ContentRootPath)
                .Returns(new DirectoryInfo(Directory.GetCurrentDirectory() + @"\..\..\..\..\..\src\MarginTrading.Backend").FullName);
            
            
            _emailService = new EmailService(
                new MustacheTemplateGenerator(mockEnvironment.Object, Path.Combine("Email", "Templates")),
                new EmailSenderClient("http://emailsender-lykke.lykke-email.svc.cluster.local", new Mock<ILog>().Object),
                new MockClientAccountService());
        }

        [Test]
        [Category("EmailSending")]
        [Category("IntegrationTests")]
        public void SendEmailMarginCall()
        {
            var testAccount = new MarginTradingAccount
            {
                Id = "TestAccountId",
                BaseAssetId = "EUR",
                LegalEntity = "DEFAULT"
            };
            _emailService.SendMarginCallEmailAsync(testAccount);
        }

        [Test]
        [Category("EmailSending")]
        [Category("IntegrationTests")]
        public void SendEmailMarginCallCyprus()
        {
            var testAccount = new MarginTradingAccount
            {
                Id = "TestAccountId",
                BaseAssetId = "EUR",
                LegalEntity = LykkeConstants.LykkeCyprusLegalEntity
            };
            _emailService.SendMarginCallEmailAsync(testAccount);
        }

        [Test]
        [Category("EmailSending")]
        [Category("IntegrationTests")]
        public void SendEmailStopOut()
        {
            var testAccount = new MarginTradingAccount
            {
                Id = "TestAccountId",
                BaseAssetId = "EUR",
                LegalEntity = "DEFAULT"
            };
            _emailService.SendStopOutEmailAsync(testAccount);
        }

        [Test]
        [Category("EmailSending")]
        [Category("IntegrationTests")]
        public void SendEmailStopOutCyprus()
        {
            var testAccount = new MarginTradingAccount
            {
                Id = "TestAccountId",
                BaseAssetId = "EUR",
                LegalEntity = LykkeConstants.LykkeCyprusLegalEntity
            };
            _emailService.SendStopOutEmailAsync(testAccount);
        }



        private class MockClientAccountService : IClientAccountService
        {
            public Task<string> GetEmail(string clientId)
            {
                return Task.FromResult("nuno.araujo@lykke.com");
            }

            public Task<string> GetNotificationId(string clientId)
            {
                throw new NotImplementedException();
            }

            public Task<bool> IsPushEnabled(string clientId)
            {
                throw new NotImplementedException();
            }
        }
    }
}
