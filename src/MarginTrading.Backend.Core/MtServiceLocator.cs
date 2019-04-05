using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Core
{
    //TODO: think about all this static mess
    public static class MtServiceLocator
    {
        public static IFplService FplService { get; set; }
        public static IAccountUpdateService AccountUpdateService { get; set; }
        public static IAccountsCacheService AccountsCacheService { get; set; }
        public static ICommissionService SwapCommissionService { get; set; }
    }
}
