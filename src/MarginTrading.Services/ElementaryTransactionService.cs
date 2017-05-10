using MarginTrading.Core;
using System;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
    public class ElementaryTransactionService : IElementaryTransactionService
    {
        private readonly IMarginTradingTransactionRepository _transactionRepository;
        private readonly IElementaryTransactionsRepository _elementaryTransactionsRepository;
        private readonly IMarginTradingAssetsRepository _assetsRepository;

        public ElementaryTransactionService(IMarginTradingTransactionRepository transactionRepostory, 
            IElementaryTransactionsRepository elementaryTransactionsRepository,
            IMarginTradingAssetsRepository assetsRepository)
        {
            _elementaryTransactionsRepository = elementaryTransactionsRepository;
            _transactionRepository = transactionRepostory;
            _assetsRepository = assetsRepository;
        }

        public async Task CreateElementaryTransactionsAsync(ITransaction transaction)
        {
            IMarginTradingAsset asset = await _assetsRepository.GetAssetAsync(transaction.CoreSymbol);

            IElementaryTransaction elementaryTransactionTakerBase = CreateTransactionForTakerBaseAsset(transaction, asset);
            await _elementaryTransactionsRepository.AddAsync(elementaryTransactionTakerBase);

            IElementaryTransaction elementaryTransactionTakerQuote = CreateTransactionForTakerQuoteAsset(transaction, asset);
            await _elementaryTransactionsRepository.AddAsync(elementaryTransactionTakerQuote);

            IElementaryTransaction elementaryTransactionMakerBase = CreateTransactionForMakerBaseAsset(transaction, asset);
            await _elementaryTransactionsRepository.AddAsync(elementaryTransactionMakerBase);

            IElementaryTransaction elementaryTransactionMakerQuote = CreateTransactionForMakerQuoteAsset(transaction, asset);
            await _elementaryTransactionsRepository.AddAsync(elementaryTransactionMakerQuote);
        }

        public async Task CreateElementaryTransactionsFromTransactionReport()
        {
            foreach (ITransaction transaction in await _transactionRepository.GetTransactionsAsync())
            {
                await CreateElementaryTransactionsAsync(transaction);
            }
        }

        private static IElementaryTransaction CreateTransactionForTakerBaseAsset(ITransaction transaction, IMarginTradingAsset asset)
        {
            var elementaryTransaction = new ElementaryTransaction();

            elementaryTransaction.AccountId = transaction.TakerAccountId;
            elementaryTransaction.Asset = asset.BaseAssetId;
            elementaryTransaction.CounterPartyId = transaction.TakerLykkeId;
            elementaryTransaction.TradingTransactionId = transaction.LykkeExecutionId;
            elementaryTransaction.TimeStamp = transaction.ExecutedTime;
            elementaryTransaction.Amount = transaction.FilledVolume *
                (transaction.CoreSide == OrderDirection.Buy ? 1 : -1);
            elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
                (transaction.CoreSide == OrderDirection.Buy ? 1 : -1);

            return elementaryTransaction;
        }

        private static IElementaryTransaction CreateTransactionForTakerQuoteAsset(ITransaction transaction, IMarginTradingAsset asset)
        {
            var elementaryTransaction = new ElementaryTransaction();

            elementaryTransaction.AccountId = transaction.TakerAccountId;
            elementaryTransaction.Asset = asset.QuoteAssetId;
            elementaryTransaction.CounterPartyId = transaction.TakerLykkeId;
            elementaryTransaction.TradingTransactionId = transaction.LykkeExecutionId;
            elementaryTransaction.TimeStamp = transaction.ExecutedTime;
            elementaryTransaction.Amount = transaction.FilledVolume * transaction.Price *
                (transaction.CoreSide == OrderDirection.Sell ? 1 : -1);
            elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
                (transaction.CoreSide == OrderDirection.Sell ? 1 : -1);

            return elementaryTransaction;
        }

        private static IElementaryTransaction CreateTransactionForMakerBaseAsset(ITransaction transaction, IMarginTradingAsset asset)
        {
            var elementaryTransaction = new ElementaryTransaction();

            elementaryTransaction.Asset = asset.BaseAssetId;
            elementaryTransaction.CounterPartyId = transaction.MakerLykkeId;
            elementaryTransaction.TradingTransactionId = transaction.LykkeExecutionId;
            elementaryTransaction.TimeStamp = transaction.ExecutedTime;
            elementaryTransaction.Amount = transaction.FilledVolume *
                (transaction.CoreSide == OrderDirection.Buy ? -1 : 1);
            elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
                (transaction.CoreSide == OrderDirection.Buy ? -1 : 1);

            return elementaryTransaction;
        }

        private static IElementaryTransaction CreateTransactionForMakerQuoteAsset(ITransaction transaction, IMarginTradingAsset asset)
        {
            var elementaryTransaction = new ElementaryTransaction();

            elementaryTransaction.Asset = asset.QuoteAssetId;
            elementaryTransaction.CounterPartyId = transaction.MakerLykkeId;
            elementaryTransaction.TradingTransactionId = transaction.LykkeExecutionId;
            elementaryTransaction.TimeStamp = transaction.ExecutedTime;
            elementaryTransaction.Amount = transaction.FilledVolume * transaction.Price *
                (transaction.CoreSide == OrderDirection.Sell ? -1 : 1);
            elementaryTransaction.AmountInUsd = transaction.VolumeInUSD *
                (transaction.CoreSide == OrderDirection.Sell ? -1 : 1);

            return elementaryTransaction;
        }

        public bool Any()
        {
            return _elementaryTransactionsRepository.Any();
        }
    }
}