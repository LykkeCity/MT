using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.SqlRepositories.Entities;
using Microsoft.Extensions.PlatformAbstractions;

namespace MarginTrading.Backend.Infrastructure
{
    public class LogToSql : ILog
    {
        private readonly ILogRepository _logRepository;

        public LogToSql(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        private async Task WriteLog(LogLevel level, string component, string process, string context, string info, 
            Exception ex = null, DateTime? dateTime = null)
        {
            var log = new LogEntity
            {
                DateTime = dateTime ?? DateTime.UtcNow,
                Level = level.ToString(),
                Env = Environment.GetEnvironmentVariable("ENV_INFO"),
                AppName = PlatformServices.Default.Application.ApplicationName,
                Version = PlatformServices.Default.Application.ApplicationVersion,
                Component = component,
                Process = process,
                Context = context,
                Type = "Message",
                Stack = Truncate(ex?.StackTrace),
                Msg = string.Join(" *** ", info, Truncate(ex?.Message)),
            };
            
            await _logRepository.Insert(log);
        }
        
        public async Task WriteInfoAsync(string component, string process, string context, string info, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Info, component, process, context, info, null, dateTime);
        }

        public async Task WriteMonitorAsync(string component, string process, string context, string info, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Monitoring, component, process, context, info, null, dateTime);
        }

        public async Task WriteWarningAsync(string component, string process, string context, string info, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Warning, component, process, context, info, null, dateTime);
        }

        public async Task WriteWarningAsync(string component, string process, string context, string info, Exception ex,
            DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Warning, component, process, context, info, ex, dateTime);
        }

        public async Task WriteErrorAsync(string component, string process, string context, Exception exception, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Error, component, process, context, null, exception, dateTime);
        }

        public async Task WriteFatalErrorAsync(string component, string process, string context, Exception exception,
            DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.FatalError, component, process, context, null, exception, dateTime);
        }

        public async Task WriteInfoAsync(string process, string context, string info, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Info, null, process, context, info, null, dateTime);
        }

        public async Task WriteMonitorAsync(string process, string context, string info, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Monitoring, null, process, context, info, null, dateTime);
        }

        public async Task WriteWarningAsync(string process, string context, string info, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Warning, null, process, context, info, null, dateTime);
        }

        public async Task WriteWarningAsync(string process, string context, string info, Exception ex, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Warning, null, process, context, info, ex, dateTime);
        }

        public async Task WriteErrorAsync(string process, string context, Exception exception, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.Error, null, process, context, null, exception, dateTime);
        }

        public async Task WriteFatalErrorAsync(string process, string context, Exception exception, DateTime? dateTime = null)
        {
            await WriteLog(LogLevel.FatalError, null, process, context, null, exception, dateTime);
        }
        
        private static string Truncate(string str)
        {
            if (str == null)
            {
                return null;
            }

            // See: https://blogs.msdn.microsoft.com/avkashchauhan/2011/11/30/how-the-size-of-an-entity-is-caclulated-in-windows-azure-table-storage/
            // String – # of Characters * 2 bytes + 4 bytes for length of string
            // Max coumn size is 64 Kb, so max string len is (65536 - 4) / 2 = 32766
            // 3 - is for "..."
            const int maxLength = 32766 - 3;

            if (str.Length > maxLength)
            {
                return string.Concat(str.Substring(0, maxLength), "...");
            }

            return str;
        }
    }
}