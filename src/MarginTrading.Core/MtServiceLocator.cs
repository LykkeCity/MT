namespace MarginTrading.Core
{
    public static class MtServiceLocator
    {
        public static IFplService FplService { get; set; }
        public static IAccountUpdateService AccountUpdateService { get; set; }
        public static IAccountsCacheService AccountsCacheService { get; set; }
        public static ICommissionService SwapCommissionService { get; set; }
    }
}
