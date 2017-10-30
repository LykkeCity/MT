﻿using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MarketMakerFeed;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Telemetry;
using MarginTrading.Common.Settings;

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

			builder.RegisterType<CommissionService>()
				.As<ICommissionService>()
				.SingleInstance();

			builder.RegisterType<ClientAccountService>()
				.As<IClientAccountService>()
				.SingleInstance();

			builder.RegisterType<InternalMatchingEngine>()
				.As<IInternalMatchingEngine>()
				.SingleInstance();

			builder.RegisterType<TradingEngine>()
				.As<ITradingEngine>()
				.As<IEventConsumer<OrderBookChangeEventArgs>>()
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

		    builder.RegisterType<AssetPairDayOffService>()
		        .As<IAssetPairDayOffService>()
		        .SingleInstance();

		    builder.RegisterType<TelemetryPublisher>()
		        .As<ITelemetryPublisher>()
		        .SingleInstance();

		    builder.RegisterType<ContextFactory>()
		        .As<IContextFactory>()
		        .SingleInstance();
			
			builder.RegisterType<RabbitMqService>()
				.As<IRabbitMqService>()
				.SingleInstance();
        }
	}
}
