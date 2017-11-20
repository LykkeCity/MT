namespace MarginTrading.Frontend.Wamp
{
    public static class WampConstants
    {
        public const string FrontEndRealmName = "mtcrossbar";

        public const string PricesTopicPrefix = "prices.update";
        public const string UserUpdatesTopicPrefix = "user";
        public const string TradesTopic = "trades";
    }
}
