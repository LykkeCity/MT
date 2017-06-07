using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Infrastructure;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Core.Notifications;
using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly ILog _log;
        private readonly ISlackNotificationsProducer _slackNotificationsProducer;
        private readonly RequestDelegate _next;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILog log, ISlackNotificationsProducer slackNotificationsProducer)
        {
            _log = log;
            _slackNotificationsProducer = slackNotificationsProducer;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var reqBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;
            try
            {
                await context.Request.Body.CopyToAsync(reqBodyStream);
                reqBodyStream.Seek(0, SeekOrigin.Begin);
                context.Request.Body = reqBodyStream;

                await _next.Invoke(context);

                context.Request.Body = originalRequestBody;
            }
            catch (Exception ex)
            {
                await LogError(context, ex, reqBodyStream);

                await SendError(context, ex.Message);
            }
        }

        private async Task LogError(HttpContext context, Exception ex, MemoryStream ms)
        {
            ms.Seek(0, SeekOrigin.Begin);

            string bodyPart;
            using (ms)
            {
                bodyPart = await GetBodyPart(ms);
            }

            await _log.WriteErrorAsync("GlobalHandler", context.Request.GetUri().AbsoluteUri, bodyPart, ex);

            var slackMsg = GetSlackMsg(context, ex, bodyPart);

            await
                _slackNotificationsProducer.SendNotification(ChannelTypes.MarginTrading, slackMsg, "MT Backend");
        }

        private string GetSlackMsg(HttpContext context, Exception ex, string bodyPart)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n====================================");
            sb.AppendLine(
                $"{context.Request.GetUri().AbsoluteUri} *{ex.GetType()}* :\n{bodyPart}\n*{ex.Message}*\n{ex.StackTrace.Substring(0, 300)}...");
            sb.AppendLine("====================================\n");

            return sb.ToString();
        }

        private const int PartSize = 1024;
        private async Task<string> GetBodyPart(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var requestReader = new StreamReader(stream);
            int len = (int)(stream.Length > PartSize ? PartSize : stream.Length);
            char[] bodyPart = new char[len];
            await requestReader.ReadAsync(bodyPart, 0, len);

            return new string(bodyPart);
        }

        private async Task SendError(HttpContext ctx, string errorMessage)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 500;
            var response = new MtBackendResponse<string>() {Result = "Technical problems", Message = errorMessage};
            await ctx.Response.WriteAsync(response.ToJson());
        }
    }
}