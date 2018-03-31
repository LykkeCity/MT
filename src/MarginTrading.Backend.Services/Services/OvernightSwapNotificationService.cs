using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common;
using Lykke.Service.ClientAccount.Client;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services.Client;

namespace MarginTrading.Backend.Services.Services
{
    public class OvernightSwapNotificationService : IOvernightSwapNotificationService
    {
        private readonly IOvernightSwapCache _overnightSwapCache;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IEmailService _emailService;
        private readonly IClientAccountService _clientAccountService;
        
        private readonly IThreadSwitcher _threadSwitcher;
        
        private readonly ILog _log;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        
        public OvernightSwapNotificationService(
            IOvernightSwapCache overnightSwapCache,
            IAccountsCacheService accountsCacheService,
            IEmailService emailService,
            IClientAccountService clientAccountService,
            
            IThreadSwitcher threadSwitcher,
            
            ILog log)
        {
            _overnightSwapCache = overnightSwapCache;
            _accountsCacheService = accountsCacheService;
            _emailService = emailService;
            _clientAccountService = clientAccountService;
            
            _threadSwitcher = threadSwitcher;

            _log = log;
        }
        
        public void PerformEmailNotification(DateTime calculationTime)
        {
            var processedCalculations = _overnightSwapCache.GetAll().Where(x => x.Time >= calculationTime).ToList();
            
            //_threadSwitcher.SwitchThread(async () =>
            Task.Run(async () =>
            {
                await _semaphore.WaitAsync();

                try
                {
                    var notifications = processedCalculations
                        .GroupBy(x => x.ClientId)
                        .Select(c => new OvernightSwapNotification
                            {
                                CliendId = c.Key,
                                CalculationsByAccount = c.GroupBy(a => a.AccountId)
                                    .Select(a =>
                                    {
                                        var account = _accountsCacheService.Get(c.Key, a.Key);
                                        if (account == null)
                                            return null;
                                        return new OvernightSwapNotification.AccountCalculations()
                                        {
                                            AccountId = a.Key,
                                            AccountCurrency = account.BaseAssetId,
                                            Calculations = a.Select(calc =>
                                                new OvernightSwapNotification.SingleCalculation
                                                {
                                                    Instrument = calc.Instrument,
                                                    Direction = calc.Direction.ToString(),
                                                    Volume = calc.Volume,
                                                    SwapRate = calc.SwapRate,
                                                    Cost = calc.Value,
                                                    PositionIds = calc.OpenOrderIds,
                                                }).ToList()
                                        };
                                    }).Where(x => x != null).ToList()
                            }
                        );

                    var clientsWithIncorrectMail = new List<string>();
                    var clientsSentEmails = new List<string>();
                    foreach (var notification in notifications)
                    {
                        try
                        {
                            var clientEmail = await _clientAccountService.GetEmail(notification.CliendId);
                            if (string.IsNullOrEmpty(clientEmail))
                            {
                                clientsWithIncorrectMail.Add(notification.CliendId);
                                continue;
                            }

                            await _emailService.SendOvernightSwapEmailAsync(clientEmail, notification);
                            clientsSentEmails.Add(notification.CliendId);
                        }
                        catch (Exception e)
                        {
                            await _log.WriteErrorAsync(nameof(OvernightSwapNotificationService),
                                nameof(PerformEmailNotification), e, DateTime.UtcNow);
                        }
                    }

                    await _log.WriteWarningAsync(nameof(OvernightSwapNotificationService), nameof(PerformEmailNotification),
                        $"Emails of some clients are incorrect: {string.Join(", ", clientsWithIncorrectMail)}.", DateTime.UtcNow);
                    await _log.WriteInfoAsync(nameof(OvernightSwapNotificationService), nameof(PerformEmailNotification),
                        $"Emails sent to: {string.Join(", ", clientsSentEmails)}.", DateTime.UtcNow);
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }
    }
}