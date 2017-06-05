using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.MarketMakerFeed;
using MarginTrading.Services.Events;

namespace MarginTrading.Services.Modules
{
	public class ServicesModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<QuoteCacheService>()
				.As<IQuoteCacheService>()
				.As<IEventConsumer<BestPriceChangeEventArgs>>()
				.SingleInstance();

			builder.RegisterType<FplService>()
				.As<IFplService>()
				.SingleInstance();

			builder.RegisterType<TradingConditionsCacheService>()
				.AsSelf()
				.As<ITradingConditionsCacheService>()
				.SingleInstance();

			builder.RegisterType<AccountAssetsCacheService>()
				.AsSelf()
				.As<IAccountAssetsCacheService>()
				.SingleInstance();

			builder.RegisterType<AccountGroupCacheService>()
				.AsSelf()
				.As<IAccountGroupCacheService>()
				.SingleInstance();

			builder.RegisterType<AccountUpdateService>()
				.As<IAccountUpdateService>()
				.SingleInstance();

			builder.RegisterType<ValidateOrderService>()
				.As<IValidateOrderService>()
				.SingleInstance();

			builder.RegisterType<SwapCommissionService>()
				.As<ISwapCommissionService>()
				.SingleInstance();

			builder.RegisterType<ClientTokenService>()
				.As<IClientTokenService>()
				.SingleInstance();

			builder.RegisterType<ClientAccountService>()
				.As<IClientAccountService>()
				.SingleInstance();

			builder.RegisterType<MatchingEngine>()
				.As<IMatchingEngine>()
				.SingleInstance();

			builder.RegisterType<TradingEngine>()
				.As<ITradingEngine>()
				.As<IEventConsumer<OrderBookChangeEventArgs>>()
				.SingleInstance();

			builder.RegisterType<MarginCallConsumer>()
				.As<IEventConsumer<MarginCallEventArgs>>()
				.SingleInstance();

			builder.RegisterType<StopOutConsumer>()
				.As<IEventConsumer<StopOutEventArgs>>()
				.SingleInstance();

			builder.RegisterType<OrderStateConsumer>()
				.As<IEventConsumer<OrderPlacedEventArgs>>()
				.As<IEventConsumer<OrderClosedEventArgs>>()
				.As<IEventConsumer<OrderCancelledEventArgs>>()
				.SingleInstance();

			builder.RegisterType<CfdCalculatorService>()
				.As<ICfdCalculatorService>()
				.SingleInstance();

			builder.RegisterType<AggregatedOrderBook>()
				.As<IEventConsumer<OrderBookChangeEventArgs>>()
				.As<IAggregatedOrderBook>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<OrderBookList>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<MarketMakerService>()
				.As<IFeedConsumer>()
				.SingleInstance();

			builder.RegisterType<MicrographCacheService>()
				.As<IEventConsumer<BestPriceChangeEventArgs>>()
				.As<IMicrographCacheService>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<OrderBookChangedConsumer>()
				.As<IEventConsumer<OrderBookChangeEventArgs>>()
				.SingleInstance();

			builder.RegisterType<MarginTradingSettingsService>()
				.As<IMarginTradingSettingsService>()
				.SingleInstance();

			builder.RegisterType<MatchingEngineRouter>()
				.As<IMatchingEngineRouter>()
				.SingleInstance();

			builder.RegisterType<MatchingEngineRoutesCacheService>()
				.As<IMatchingEngineRoutesCacheService>()
				.AsSelf()
				.SingleInstance();
		}
	}
}
