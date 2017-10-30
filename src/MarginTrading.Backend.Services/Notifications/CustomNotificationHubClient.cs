using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Notifications
{
    public class CustomNotificationHubClient
    {
        private readonly string _sharedAccessKey;
        private readonly string _sharedAccessKeyName;
        private readonly string _url;

        public CustomNotificationHubClient(string sharedAccessKey, string sharedAccessKeyName, string baseUrl, string hubName)
        {
            _sharedAccessKey = sharedAccessKey;
            _sharedAccessKeyName = sharedAccessKeyName;
            _url = string.Format("https://{0}/{1}/messages/?api-version=2015-08", baseUrl, hubName);
        }

        public static CustomNotificationHubClient CreateClientFromConnectionString(string connectionString,
            string hubName)
        {
            var regexp = new Regex(@"sb://(?<url>[A-z\.\-]*)/;SharedAccessKeyName=(?<keyName>[A-z0-9]*);.*SharedAccessKey=(?<key>[A-z0-9+=/]*)");
            var match = regexp.Match(connectionString);
            var baseUrl = match.Groups["url"].Value;
            var accessKey = match.Groups["key"].Value;
            var accessKeyName = match.Groups["keyName"].Value;

            return new CustomNotificationHubClient(accessKey, accessKeyName, baseUrl, hubName);
        }

        public async Task SendGcmNativeNotificationAsync(string jsonPayload, string[] ids)
        {
            var headers = new Dictionary<string, string>
            {
                {"ServiceBusNotification-Format", "gcm"},
                {"ServiceBusNotification-Tags", string.Join("||", ids)}
            };

            await SendNotification(jsonPayload, headers);
        }

        public async Task SendAppleNativeNotificationAsync(string jsonPayload, string[] ids)
        {
            var headers = new Dictionary<string, string>
            {
                {"ServiceBusNotification-Format", "apple"},
                {"ServiceBusNotification-Tags", string.Join("||", ids)},
                {"ServiceBusNotification-Apns-Expiry", DateTime.UtcNow.AddDays(10).ToString("s")}
            };

            await SendNotification(jsonPayload, headers);
        }

        public async Task SendNotification(string payload, Dictionary<string, string> headers)
        {
            var request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "POST";
            request.ContentType = "application/json;charset=utf-8";

            var epochTime = (long)(DateTime.UtcNow - new DateTime(1970, 01, 01)).TotalSeconds;
            var expiry = epochTime + (long)TimeSpan.FromHours(1).TotalSeconds;

            var encodedUrl = WebUtility.UrlEncode(_url);
            var stringToSign = encodedUrl + "\n" + expiry;
            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_sharedAccessKey));

            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = $"SharedAccessSignature sr={encodedUrl}&sig={WebUtility.UrlEncode(signature)}&se={expiry}&skn={_sharedAccessKeyName}";

            request.Headers[HttpRequestHeader.Authorization] = sasToken;

            foreach (var header in headers)
                request.Headers[header.Key] = header.Value;

            using (var stream = await request.GetRequestStreamAsync())
            using (var streamWriter = new StreamWriter(stream))
            {
                streamWriter.Write(payload);
                streamWriter.Flush();
            }

            await request.GetResponseAsync();
        }
    }
}
