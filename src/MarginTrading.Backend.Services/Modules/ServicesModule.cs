// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using Common.Log;
using Autofac.Features.Variance;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Correlation.RabbitMq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.EventsConsumers;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.Scheduling;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Backend.Services.Workflow.Liquidation;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Telemetry;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

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
				.SingleInstance();
 
			builder.RegisterType<FxRateCacheService>() 
				.AsSelf()
				.As<IFxRateCacheService>()
				.SingleInstance(); 

			builder.RegisterType<FplService>()
				.As<IFplService>()
				.InstancePerLifetimeScope();

            builder.RegisterType<TradingInstrumentsCacheService>()
				.AsSelf()
				.As<ITradingInstrumentsCacheService>()
				.As<IOvernightMarginParameterContainer>()
				.SingleInstance();

			builder.RegisterType<AccountUpdateService>()
				.As<IAccountUpdateService>()
				.InstancePerLifetimeScope();

			builder.RegisterType<OrderValidator>()
				.As<IOrderValidator>()
				.SingleInstance();

			builder.RegisterType<CommissionService>()
				.As<ICommissionService>()
				.SingleInstance();

			//TODO: rework ME registrations
			builder.RegisterType<MarketMakerMatchingEngine>()
				.As<IMarketMakerMatchingEngine>()
				.WithParameter(TypedParameter.From(MatchingEngineConstants.DefaultMm))
				.SingleInstance();

			builder.RegisterType<StpMatchingEngine>()
				.As<IStpMatchingEngine>()
				.WithParameter(TypedParameter.From(MatchingEngineConstants.DefaultStp))
				.SingleInstance();

			builder.RegisterType<TradingEngine>()
				.As<ITradingEngine>()
				.As<IEventConsumer<BestPriceChangeEventArgs>>()
				.As<IEventConsumer<FxBestPriceChangeEventArgs>>()
				.SingleInstance();

			builder.RegisterType<MarginCallConsumer>()
				.As<IEventConsumer<MarginCallEventArgs>>()
				//.As<IEventConsumer<OrderPlacedEventArgs>>()
				.SingleInstance();

			builder.RegisterType<StopOutConsumer>()
				.As<IEventConsumer<StopOutEventArgs>>()
				.SingleInstance();

			builder.RegisterSource(new ContravariantRegistrationSource());
			builder.RegisterType<OrderStateConsumer>()
				.As<IEventConsumer<OrderPlacedEventArgs>>()
				.As<IEventConsumer<OrderExecutedEventArgs>>()
				.As<IEventConsumer<OrderCancelledEventArgs>>()
				.As<IEventConsumer<OrderChangedEventArgs>>()
				.As<IEventConsumer<OrderExecutionStartedEventArgs>>()
				.As<IEventConsumer<OrderActivatedEventArgs>>()
				.As<IEventConsumer<OrderRejectedEventArgs>>()
				.SingleInstance();

			builder.RegisterType<TradesConsumer>()
				.As<IEventConsumer<OrderExecutedEventArgs>>()
				.SingleInstance();
			
			builder.RegisterType<PositionsConsumer>()
				.As<IEventConsumer<OrderExecutedEventArgs>>()
				.SingleInstance();

			builder.RegisterType<CfdCalculatorService>()
				.As<ICfdCalculatorService>()
				.SingleInstance();

			builder.RegisterType<OrderBookList>()
				.AsSelf()
				.SingleInstance();

			builder.RegisterType<LightweightExternalOrderbookService>()
				.As<IExternalOrderbookService>()
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
					return new RabbitMqService(
						c.Resolve<ILoggerFactory>(),
						c.Resolve<ILog>(),
						c.Resolve<IConsole>(),
						settings.Nested(s => s.Db.StateConnString),
						settings.CurrentValue.Env,
						c.Resolve<IPublishingQueueRepository>(),
						c.Resolve<RabbitMqCorrelationManager>());
				})
				.As<IRabbitMqService>()
				.SingleInstance();

			builder.RegisterType<ScheduleSettingsCacheService>()
				.As<IScheduleSettingsCacheService>()
				.SingleInstance();

			builder.RegisterType<AlertSeverityLevelService>()
				.As<IAlertSeverityLevelService>()
				.SingleInstance();

			builder.RegisterType<ReportService>()
				.As<IReportService>()
				.SingleInstance();

			builder.RegisterType<OvernightMarginService>()
				.As<IOvernightMarginService>()
				.SingleInstance();

			builder.RegisterType<ScheduleControlService>()
				.As<IScheduleControlService>()
				.SingleInstance();

			builder.RegisterType<ScheduleSettingsCacheWarmUpJob>()
				.SingleInstance();

			builder.RegisterType<LiquidationHelper>()
				.SingleInstance();

			builder.RegisterType<LiquidationFailureExecutor>()
				.As<ILiquidationFailureExecutor>()
				.SingleInstance();

            builder.RegisterType<BrokerSettingsChangedHandler>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<PositionsProvider>()
	            .As<IPositionsProvider>()
	            .InstancePerLifetimeScope();

            builder.RegisterType<OrdersProvider>()
	            .As<IOrdersProvider>()
	            .InstancePerLifetimeScope();

            builder.RegisterType<AccountsProvider>()
	            .As<IAccountsProvider>()
	            .InstancePerLifetimeScope();

            builder.RegisterType<FinalSnapshotCalculator>()
	            .As<IFinalSnapshotCalculator>()
	            .InstancePerLifetimeScope();
            
            // @atarutin: DraftSnapshotKeeper implements IOrderReader interface for convenient access to positions
            // and orders but it is not required to be used for registration in DI container
            builder.RegisterType<DraftSnapshotKeeper>()
	            .As<IDraftSnapshotKeeper>()
	            .InstancePerLifetimeScope();
            
            builder.RegisterType<SystemClock>().As<ISystemClock>().SingleInstance();

            builder.RegisterType<PositionHistoryHandler>()
	            .As<IPositionHistoryHandler>()
	            .SingleInstance();
		}
	}
}
