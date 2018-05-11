namespace MarginTrading.Common.Json
{
    public class CacheSerializer
    {
        public static T Deserialize<T>(byte[] settings)
        {
            return MessagePack.MessagePackSerializer.Deserialize<T>(settings, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        public static byte[] Serialize<T>(T settings)
        {
            return MessagePack.MessagePackSerializer.Serialize(settings, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }
    }
}