using WampSharp.V2.Authentication;

namespace MarginTrading.Frontend.Wamp
{
    public class WampSessionAuthenticatorFactory : IWampSessionAuthenticatorFactory
    {
        public IWampSessionAuthenticator GetSessionAuthenticator
        (WampPendingClientDetails details,
            IWampSessionAuthenticator transportAuthenticator)
        {
            return new AnonymousWampSessionAuthenticator();
        }
    }
}