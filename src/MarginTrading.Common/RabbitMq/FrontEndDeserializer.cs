using System.Text;
using Lykke.RabbitMqBroker.Subscriber;
using Newtonsoft.Json;

namespace MarginTrading.Common.RabbitMq
{
    public class FrontEndDeserializer<T> : IMessageDeserializer<T>
    {
        public T Deserialize(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}