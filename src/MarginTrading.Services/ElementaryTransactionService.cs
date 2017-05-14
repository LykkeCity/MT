using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
	public class ElementaryTransactionService : IElementaryTransactionService
	{
		private readonly IMarginTradingAssetsRepository _assetsRepository;

		public ElementaryTransactionService(IMarginTradingAssetsRepository assetsRepository)
		{
			_assetsRepository = assetsRepository;
		}

		public async Task CreateElementaryTransactionsAsync(ITransaction transaction, Func<IElementaryTransaction, Task> destination)
		{
			IMarginTradingAsset asset = await _assetsRepository.GetAssetAsync(transaction.CoreSymbol);

			IElementaryTransaction elementaryTransactionTakerBase = CreateTransactionForTakerBaseAsset(transaction, asset);
			await destination(elementaryTransactionTakerBase);

			IElementaryTransaction elementaryTransactionTakerQuote = CreateTransactionForTakerQuoteAsset(transaction, asset);
			await destination(elementaryTransactionTakerQuote);

			IElementaryTransaction elementaryTransactionMakerBase = CreateTransactionForMakerBaseAsset(transaction, asset);
			await destination(elementaryTransactionMakerBase);

			IElementaryTransaction elementaryTransactionMakerQuote = CreateTransactionForMakerQuoteAsset(transaction, asset);
			await destination(elementaryTransactionMakerQuote);

			if (transaction.ExchangeMarkup != null && transaction.ExchangeMarkup != 0)
			{
				IElementaryTransaction commissionTakerBaseTransaction = CreateTransactionForTakerCommission(transaction, asset);
				await destination(commissionTakerBaseTransaction);

				IElementaryTransaction commissionMakerBaseTransaction = CreateTransactionForMakerCommission(transaction, asset);
				await destination(commissionTakerBaseTransaction);
			}

			if (transaction.TakerProfit != null && transaction.TakerProfit != null)
			{
				IElementaryTransaction makerReducePositionTransaction = CreateTransactionForDeductionOfProfitFromMakerPosition(transaction, asset);
				await destination(makerReducePositionTransaction);

				IElementaryTransaction makerIncreaseProfitTransaction = CreateTransactionForTransferOfProfitToMakerCapital(transaction, asset);
				await destination(makerIncreaseProfitTransaction);

				IElementaryTransaction takerReducePositionTransaction = CreateTransactionForDeductionOfProfitFromTakerPosition(transaction, asset);
				await destination(takerReducePositionTransaction);

				IElementaryTransaction takerIncreaseProfitTransaction = CreateTransactionForTransferOfProfitToTakerCapital(transaction, asset);
				await destination(takerIncreaseProfitTransaction);
			}
		}

		private IElementaryTransaction CreateTransactionForTransferOfProfitToMakerCapital(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.QuoteAssetId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Quote;
			elementaryTransaction.CounterPartyId = transaction.MakerCounterpartyId;
			elementaryTransaction.AccountId = transaction.MakerAccountId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Amount = -transaction.TakerProfit;
			elementaryTransaction.Type = ElementaryTransactionType.Capital;
			elementaryTransaction.SubType = "Maker_IncreaseCapital";

			return elementaryTransaction;
		}

		private IElementaryTransaction CreateTransactionForDeductionOfProfitFromMakerPosition(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.QuoteAssetId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Quote;
			elementaryTransaction.CounterPartyId = transaction.MakerCounterpartyId;
			elementaryTransaction.AccountId = transaction.MakerAccountId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Amount = transaction.TakerProfit;
			elementaryTransaction.Type = ElementaryTransactionType.Position;
			elementaryTransaction.SubType = "Maker_ReducePosition";

			return elementaryTransaction;
		}

		private IElementaryTransaction CreateTransactionForTransferOfProfitToTakerCapital(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.QuoteAssetId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Quote;
			elementaryTransaction.AccountId = transaction.TakerAccountId;
			elementaryTransaction.CounterPartyId = transaction.TakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Amount = transaction.TakerProfit;
			elementaryTransaction.Type = ElementaryTransactionType.Capital;
			elementaryTransaction.SubType = "Taker_IncreaseCapital";

			return elementaryTransaction;
		}

		private IElementaryTransaction CreateTransactionForDeductionOfProfitFromTakerPosition(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.QuoteAssetId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Quote;
			elementaryTransaction.AccountId = transaction.TakerAccountId;
			elementaryTransaction.CounterPartyId = transaction.TakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Amount = -transaction.TakerProfit;
			elementaryTransaction.Type = ElementaryTransactionType.Position;
			elementaryTransaction.SubType = "Taker_ReducePosition";

			return elementaryTransaction;
		}

		public async Task CreateElementaryTransactionsFromTransactionReport(Func<Task<IEnumerable<ITransaction>>> source, Func<IElementaryTransaction, Task> destination)
		{
			foreach (ITransaction transaction in await source())
			{
				await CreateElementaryTransactionsAsync(transaction, destination);
			}
		}

		private static IElementaryTransaction CreateTransactionForTakerBaseAsset(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.AccountId = transaction.TakerAccountId;
			elementaryTransaction.Asset = asset.BaseAssetId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Base;
			elementaryTransaction.CounterPartyId = transaction.TakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Type = ElementaryTransactionType.Position;
			elementaryTransaction.Amount = transaction.FilledVolume *
				(transaction.CoreSide == OrderDirection.Buy ? 1 : -1);
			elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
				(transaction.CoreSide == OrderDirection.Buy ? 1 : -1);
			elementaryTransaction.SubType = "Taker";

			return elementaryTransaction;
		}

		private static IElementaryTransaction CreateTransactionForTakerQuoteAsset(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.AccountId = transaction.TakerAccountId;
			elementaryTransaction.Asset = asset.QuoteAssetId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Quote;
			elementaryTransaction.CounterPartyId = transaction.TakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Type = ElementaryTransactionType.Position;
			elementaryTransaction.Amount = transaction.FilledVolume * transaction.Price *
				(transaction.CoreSide == OrderDirection.Sell ? 1 : -1);
			elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
				(transaction.CoreSide == OrderDirection.Sell ? 1 : -1);
			elementaryTransaction.SubType = "Taker";

			return elementaryTransaction;
		}

		private static IElementaryTransaction CreateTransactionForMakerBaseAsset(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.BaseAssetId;
			elementaryTransaction.AccountId = transaction.MakerAccountId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Base;
			elementaryTransaction.CounterPartyId = transaction.MakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Type = ElementaryTransactionType.Position;
			elementaryTransaction.Amount = transaction.FilledVolume *
				(transaction.CoreSide == OrderDirection.Buy ? -1 : 1);
			elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
				(transaction.CoreSide == OrderDirection.Buy ? -1 : 1);
			elementaryTransaction.SubType = "Maker";

			return elementaryTransaction;
		}

		private static IElementaryTransaction CreateTransactionForMakerQuoteAsset(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.QuoteAssetId;
			elementaryTransaction.AccountId = transaction.MakerAccountId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Quote;
			elementaryTransaction.CounterPartyId = transaction.MakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Type = ElementaryTransactionType.Position;
			elementaryTransaction.Amount = transaction.FilledVolume * transaction.Price *
				(transaction.CoreSide == OrderDirection.Sell ? -1 : 1);
			elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
				(transaction.CoreSide == OrderDirection.Sell ? -1 : 1);
			elementaryTransaction.SubType = "Maker";

			return elementaryTransaction;
		}

		private static IElementaryTransaction CreateTransactionForTakerCommission(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.BaseAssetId;
			elementaryTransaction.AccountId = transaction.TakerAccountId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Base;
			elementaryTransaction.CounterPartyId = transaction.TakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Amount = -transaction.ExchangeMarkup;
			elementaryTransaction.Type = ElementaryTransactionType.Position;
			elementaryTransaction.SubType = "TakerCommission";

			return elementaryTransaction;
		}

		private static IElementaryTransaction CreateTransactionForMakerCommission(ITransaction transaction, IMarginTradingAsset asset)
		{
			var elementaryTransaction = new ElementaryTransaction();

			elementaryTransaction.Asset = asset.BaseAssetId;
			elementaryTransaction.CoreSymbol = transaction.CoreSymbol;
			elementaryTransaction.AssetSide = AssetSide.Base;
			elementaryTransaction.AccountId = transaction.MakerAccountId;
			elementaryTransaction.CounterPartyId = transaction.MakerCounterpartyId;
			elementaryTransaction.TradingTransactionId = transaction.TradingTransactionId;
			elementaryTransaction.TradingOrderId = transaction.TakerOrderId;
			elementaryTransaction.PositionId = transaction.TakerPositionId;
			elementaryTransaction.TimeStamp = transaction.ExecutedTime;
			elementaryTransaction.Amount = transaction.ExchangeMarkup;
			elementaryTransaction.Type = ElementaryTransactionType.Capital;
			elementaryTransaction.SubType = "MakerCommission";

			return elementaryTransaction;
		}
	}
}