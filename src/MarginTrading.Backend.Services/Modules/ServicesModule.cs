using Autofac;
using Common.Log;
using Autofac.Features.Variance;
using Lykke.SettingsReader;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.EventsConsumers;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Common.Services.Telemetry;

namespace MarginTrading.Backend.Services.Modules
{
	public class ServicesModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<QuoteCacheService>()
				.AsSelf()
				.As<IQuoteCacheService>()
				.As<IEventConsumer<BestPriceChangeEventArgs>>()
				.SingleInstance()
				.OnActivated(args => args.Instance.Start());

			builder.RegisterType<FplService>()
				.As<IFplService>()
				.SingleInstance();

			builder.RegisterType<TradingConditionsCacheService>()
				.AsSelf()
				.As<ITradingConditionsCacheService>()
				.SingleInstance();

			builder.RegisterType<TradingInstrumnentsCacheService>()
				.AsSelf()
				.As<ITradingInstrumnentsCacheService>()
				.SingleInstance();

			builder.RegisterType<AccountUpdateService>()
				.As<IAccountUpdateService>()
				.SingleInstance();

			builder.RegisterType<ValidateOrderService>()
				.As<IValidateOrderService>()
				.SingleInstance();

			builder.RegisterType<CommissionService>()
				.As<ICommissionService>()
				.SingleInstance();

			builder.RegisterType<MarketMakerMatchingEngine>()
				.As<IMarketMakerMatchingEngine>()
				.WithParameter(TypedParameter.From(MatchingEngineConstants.LykkeVuMm))
				.SingleInstance();

			builder.RegisterType<StpMatchingEngine>()
				.As<IStpMatchingEngine>()
				.WithParameter(TypedParameter.From(MatchingEngineConstants.LykkeCyStp))
				.SingleInstance();

			builder.RegisterType<TradingEngine>()
				.As<ITradingEngine>()
				.As<IEventConsumer<BestPriceChangeEventArgs>>()
				.SingleInstance();

			builder.RegisterType<MarginCallConsumer>()
				.As<IEventConsumer<MarginCallEventArgs>>()
				.As<IEventConsumer<OrderPlacedEventArgs>>()
				.As<IEventConsumer<OrderClosedEventArgs>>()
				.As<IEventConsumer<OrderCancelledEventArgs>>()
				.SingleInstance();

			builder.RegisterType<StopOutConsumer>()
				.As<IEventConsumer<StopOutEventArgs>>()
				.SingleInstance();

			builder.RegisterSource(new ContravariantRegistrationSource());
			builder.RegisterType<OrderStateConsumer>()
				.As<IEventConsumer<OrderPlacedEventArgs>>()
				.As<IEventConsumer<OrderClosedEventArgs>>()
				.As<IEventConsumer<OrderCancelledEventArgs>>()
				.As<IEventConsumer<OrderLimitsChangedEventArgs>>()
				.As<IEventConsumer<OrderClosingEventArgs>>()
				.As<IEventConsumer<OrderActivatedEventArgs>>()
				.As<IEventConsumer<OrderRejectedEventArgs>>()
				.SingleInstance();

			builder.RegisterType<TradesConsumer>()
				.As<IEventConsumer<OrderPlacedEventArgs>>()
				.As<IEventConsumer<OrderClosedEventArgs>>()
				.SingleInstance();

			builder.RegisterType<CfdCalculatorService>()
				.As<ICfdCalculatorService>()
				.SingleInstance();

			builder.RegisterType<OrderBookList>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<ExternalOrderBooksList>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<MarketMakerService>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<MarginTradingEnabledCacheService>()
				.As<IMarginTradingSettingsCacheService>()
				.SingleInstance();

			builder.RegisterType<MatchingEngineRouter>()
				.As<IMatchingEngineRouter>()
				.SingleInstance();

			builder.RegisterType<MatchingEngineRoutesCacheService>()
				.As<IMatchingEngineRoutesCacheService>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<AssetPairDayOffService>()
				.As<IAssetPairDayOffService>()
				.SingleInstance();

			builder.RegisterType<TelemetryPublisher>()
				.As<ITelemetryPublisher>()
				.SingleInstance();

			builder.RegisterType<ContextFactory>()
				.As<IContextFactory>()
				.SingleInstance();

			builder.Register(c =>
				{
					var settings = c.Resolve<IReloadingManager<MarginTradingSettings>>();
					return new RabbitMqService(c.Resolve<ILog>(), c.Resolve<IConsole>(),
						settings.Nested(s => s.Db.StateConnString), settings.CurrentValue.Env);
				})
				.As<IRabbitMqService>()
				.SingleInstance();

			builder.RegisterType<DayOffSettingsService>()
				.As<IDayOffSettingsService>()
				.As<IStartable>()
				.SingleInstance();

			builder.RegisterType<AlertSeverityLevelService>()
				.As<IAlertSeverityLevelService>()
				.SingleInstance();

			builder.RegisterType<MarginTradingEnablingService>()
				.As<IMarginTradingEnablingService>()
				.As<IStartable>()
				.SingleInstance();
		}
	}
}
