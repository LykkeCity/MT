using System;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Common.Enums;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Stubs
{
    public class MtSlackNotificationsSenderLogStub : IMtSlackNotificationsSender
    {
        private readonly string _appName;
        private readonly string _env;
        private readonly ILog _consoleLog;

        public MtSlackNotificationsSenderLogStub(string appName, string env, ILog consoleLog)
        {
            _appName = appName;
            _env = env;
            _consoleLog = consoleLog;
        }

        public async Task SendAsync(string type, string sender, string message)
        {
            if (type.Equals(ChannelTypes.Monitor, StringComparison.InvariantCultureIgnoreCase))
            {
                await _consoleLog.WriteInfoAsync(sender, type, message);
                return;
            }

            await _consoleLog.WriteInfoAsync(sender, ChannelTypes.MarginTrading, GetSlackMsg(message));
        }

        public async Task SendAsync(DateTime moment, string type, string sender, string message)
        {
            if (type.Equals(ChannelTypes.Monitor, StringComparison.InvariantCultureIgnoreCase))
            {
                await _consoleLog.WriteInfoAsync(sender, type, message, moment);
                return;
            }

            await _consoleLog.WriteInfoAsync(sender, ChannelTypes.MarginTrading, GetSlackMsg(message), moment);
        }

        public Task SendRawAsync(string type, string sender, string message)
        {
            return _consoleLog.WriteInfoAsync(sender, type, message);
        }

        private string GetSlackMsg(string message)
        {
            var sb = new StringBuilder();
            sb.Append(_appName);
            sb.Append(":");
            sb.AppendLine(_env);
            sb.AppendLine("\n====================================");
            sb.AppendLine(message);
            sb.AppendLine("====================================\n");

            return sb.ToString();
        }
    }
}