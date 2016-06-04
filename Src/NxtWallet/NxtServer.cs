﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using NxtLib;
using NxtLib.Accounts;
using NxtLib.AssetExchange;
using NxtLib.Blocks;
using NxtLib.Local;
using NxtLib.MonetarySystem;
using NxtLib.ServerInfo;
using NxtLib.Transactions;
using NxtWallet.Model;
using NxtWallet.ViewModel.Model;
using Asset = NxtWallet.ViewModel.Model.Asset;
using Transaction = NxtWallet.ViewModel.Model.Transaction;
using TransactionType = NxtWallet.ViewModel.Model.TransactionType;
using NxtLib.Shuffling;

namespace NxtWallet
{
    public interface INxtServer
    {
        event PropertyChangedEventHandler PropertyChanged;

        bool IsOnline { get; }

        Task<BlockchainStatus> GetBlockchainStatusAsync();
        Task<Block<ulong>> GetBlockAsync(ulong blockId);
        Task<Block<ulong>> GetBlockAsync(int height);
        Task<long> GetUnconfirmedNqtBalanceAsync();
        Task<IEnumerable<Transaction>> GetTransactionsAsync(DateTime lastTimestamp);
        Task<IEnumerable<Transaction>> GetTransactionsAsync();
        Task<IEnumerable<Transaction>> GetDividendTransactionsAsync(string account, DateTime timestamp);
        Task<Result<Transaction>> SendMoneyAsync(Account recipient, Amount amount, string message);
        Task<IEnumerable<Transaction>> GetAssetTradesAsync(DateTime timestamp);
        Task<IEnumerable<MsCurrencyExchangeTransaction>> GetExchanges(DateTime timestamp);
        Task<Asset> GetAssetAsync(ulong assetId);
        void UpdateNxtServer(string newServerAddress);
        Task<IEnumerable<Transaction>> GetForgingIncomeAsync(DateTime timestamp);
        Task<Transaction> GetTransactionAsync(ulong transactionId);
        Task<bool> GetIsPurchaseExpired(ulong purchaseId);
        Task<Currency> GetCurrencyAsync(ulong currencyId);
        Task<ShufflingData> GetShuffling(ulong shufflingId);
    }

    public class NxtServer : ObservableObject, INxtServer
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IMapper _mapper;
        private bool _isOnline;
        private IServiceFactory _serviceFactory;
        private ulong requireBlock => _walletRepository.LastBalanceMatchBlockId;

        public bool IsOnline
        {
            get { return _isOnline; }
            set { Set(ref _isOnline, value); }
        }

        public NxtServer(IWalletRepository walletRepository, IMapper mapper, IServiceFactory serviceFactory)
        {
            _walletRepository = walletRepository;
            _mapper = mapper;
            IsOnline = false;
            _serviceFactory = serviceFactory;
        }

        public async Task<BlockchainStatus> GetBlockchainStatusAsync()
        {
            try
            {
                var serverInfoService = _serviceFactory.CreateServerInfoService();
                var blockchainStatus = await serverInfoService.GetBlockchainStatus();
                IsOnline = true;
                return blockchainStatus;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<Block<ulong>> GetBlockAsync(ulong blockId)
        {
            try
            {
                var blockService = _serviceFactory.CreateBlockService();
                var blockReply = await blockService.GetBlock(BlockLocator.ByBlockId(blockId), requireBlock: requireBlock);
                IsOnline = true;
                return blockReply;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<Block<ulong>> GetBlockAsync(int height)
        {
            try
            {
                var blockService = _serviceFactory.CreateBlockService();
                var blockReply = await blockService.GetBlock(BlockLocator.ByHeight(height), requireBlock: requireBlock);
                IsOnline = true;
                return blockReply;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<long> GetUnconfirmedNqtBalanceAsync()
        {
            try
            {
                var accountService = _serviceFactory.CreateAccountService();
                var balanceResult = await accountService.GetBalance(_walletRepository.NxtAccount, requireBlock: requireBlock);
                IsOnline = true;
                return balanceResult.UnconfirmedBalance.Nqt;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
            catch (NxtException e)
            {
                if (!e.Message.Equals("Unknown account"))
                {
                    IsOnline = false;
                    throw new Exception("", e);
                }
            }
            IsOnline = true;
            return 0;
        }

        public async Task<Transaction> GetTransactionAsync(ulong transactionId)
        {
            try
            {
                var transactionService = _serviceFactory.CreateTransactionService();
                var transactionReply = await transactionService.GetTransaction(GetTransactionLocator.ByTransactionId(transactionId),
                    requireBlock: requireBlock);
                IsOnline = true;
                return _mapper.Map<Transaction>(transactionReply);
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<bool> GetIsPurchaseExpired(ulong purchaseId)
        {
            try
            {
                var dgsService = _serviceFactory.CreateDigitalGoodsStoreService();
                var purchaseReply = await dgsService.GetPurchase(purchaseId, requireBlock: requireBlock);
                IsOnline = true;
                return purchaseReply.Pending == false && purchaseReply.GoodsData == null;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<Currency> GetCurrencyAsync(ulong currencyId)
        {
            try
            {
                var msService = _serviceFactory.CreateMonetarySystemService();
                var currency = await msService.GetCurrency(CurrencyLocator.ByCurrencyId(currencyId), requireBlock: requireBlock);
                IsOnline = true;
                return currency;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(DateTime lastTimestamp)
        {
            var transactionList = new List<Transaction>();
            try
            {
                var transactionService = _serviceFactory.CreateTransactionService();
                var transactionsTask = transactionService.GetBlockchainTransactions(_walletRepository.NxtAccount, 
                    lastTimestamp, requireBlock: requireBlock);
                var unconfirmedTask = transactionService.GetUnconfirmedTransactions(new[] {_walletRepository.NxtAccount},
                    requireBlock);

                await Task.WhenAll(transactionsTask, unconfirmedTask);

                transactionList.AddRange(_mapper.Map<List<Transaction>>(transactionsTask.Result.Transactions));
                transactionList.AddRange(_mapper.Map<List<Transaction>>(unconfirmedTask.Result.UnconfirmedTransactions));
                IsOnline = true;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
            return transactionList.OrderByDescending(t => t.Timestamp);
        }

        public Task<IEnumerable<Transaction>> GetTransactionsAsync()
        {
            return GetTransactionsAsync(new DateTime(2013, 11, 24, 12, 0, 0, DateTimeKind.Utc));
        }

        public async Task<IEnumerable<Transaction>> GetDividendTransactionsAsync(string account, DateTime timestamp)
        {
            try
            {
                var transactionService = _serviceFactory.CreateTransactionService();
                var transactions = await transactionService.GetBlockchainTransactions(
                    account, timestamp, TransactionSubType.ColoredCoinsDividendPayment, requireBlock: requireBlock);
                IsOnline = true;
                return _mapper.Map<IEnumerable<Transaction>>(transactions.Transactions);
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        //TODO: Use one known exception instead of Result<>
        public async Task<Result<Transaction>> SendMoneyAsync(Account recipient, Amount amount, string message)
        {
            try
            {
                var accountService = _serviceFactory.CreateAccountService();
                var transactionService = _serviceFactory.CreateTransactionService();
                var localTransactionService = new LocalTransactionService();

                var sendMoneyReply = await CreateUnsignedSendMoneyReply(recipient, amount, message, accountService);
                var signedTransaction = localTransactionService.SignTransaction(sendMoneyReply, _walletRepository.SecretPhrase);
                var broadcastReply = await transactionService.BroadcastTransaction(new TransactionParameter(signedTransaction.ToString()));

                IsOnline = true;

                var transaction = _mapper.Map<Transaction>(sendMoneyReply.Transaction);
                transaction.NxtId = broadcastReply.TransactionId;
                return new Result<Transaction>(transaction);
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<Asset> GetAssetAsync(ulong assetId)
        {
            try
            {
                var assetService = _serviceFactory.CreateAssetExchangeService();
                var asset = await assetService.GetAsset(assetId, requireBlock: requireBlock);
                IsOnline = true;
                return _mapper.Map<Asset>(asset);
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<IEnumerable<Transaction>> GetAssetTradesAsync(DateTime timestamp)
        {
            try
            {
                var assetService = _serviceFactory.CreateAssetExchangeService();
                var trades = await assetService.GetTrades(
                    AssetIdOrAccountId.ByAccountId(_walletRepository.NxtAccount), timestamp: timestamp, requireBlock: requireBlock);
                IsOnline = true;
                return _mapper.Map<IEnumerable<AssetTradeTransaction>>(trades.Trades);
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<IEnumerable<MsCurrencyExchangeTransaction>> GetExchanges(DateTime timestamp)
        {
            try
            {
                var msService = _serviceFactory.CreateMonetarySystemService();
                var exchanges = await msService.GetExchanges(
                    CurrencyOrAccountLocator.ByAccountId(_walletRepository.NxtAccount.AccountRs), 
                    timestamp: timestamp, includeCurrencyInfo: true, requireBlock: requireBlock);
                IsOnline = true;
                return _mapper.Map<IEnumerable<MsCurrencyExchangeTransaction>>(exchanges.Exchanges);
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public async Task<ShufflingData> GetShuffling(ulong shufflingId)
        {
            try
            {
                var shufflingService = _serviceFactory.CreateShufflingService();
                var shuffling = await shufflingService.GetShuffling(shufflingId, false, requireBlock);
                IsOnline = true;
                return shuffling;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        public void UpdateNxtServer(string newServerAddress)
        {
            _serviceFactory = new ServiceFactory(newServerAddress);
        }

        public async Task<IEnumerable<Transaction>> GetForgingIncomeAsync(DateTime timestamp)
        {
            try
            {
                var assetService = _serviceFactory.CreateAccountService();
                var accountBlocks = await assetService.GetAccountBlocks(_walletRepository.NxtAccount.AccountRs, timestamp, requireBlock: requireBlock);

                var transactions = accountBlocks.Blocks
                    .Where(b => b.TotalFee.Nqt > 0)
                    .Select(block => new Transaction
                {
                    AccountFrom = Transaction.GeneratedFromAddress,
                    AccountTo = _walletRepository.NxtAccount.AccountRs,
                    Height = block.Height,
                    IsConfirmed = true,
                    NqtAmount = block.TotalFee.Nqt,
                    NqtFee = 0,
                    NxtId = block.BlockId,
                    Timestamp = block.Timestamp,
                    TransactionType = TransactionType.ForgeIncome,
                    Message = "[Forge Income]",
                    UserIsTransactionRecipient = true,
                    UserIsTransactionSender = false
                });

                IsOnline = true;
                return transactions;
            }
            catch (HttpRequestException e)
            {
                IsOnline = false;
                throw new Exception("Error when connecting to nxt server", e);
            }
            catch (JsonReaderException e)
            {
                IsOnline = false;
                throw new Exception("Error when parsing response", e);
            }
        }

        private async Task<TransactionCreatedReply> CreateUnsignedSendMoneyReply(Account recipient, Amount amount, string message,
            IAccountService accountService)
        {
            var createTransactionByPublicKey = new CreateTransactionByPublicKey(1440, Amount.Zero, _walletRepository.NxtAccountWithPublicKey.PublicKey);
            if (!string.IsNullOrEmpty(message))
            {
                createTransactionByPublicKey.Message = new CreateTransactionParameters.UnencryptedMessage(message);
            }
            var sendMoneyReply = await accountService.SendMoney(createTransactionByPublicKey, recipient, amount);
            return sendMoneyReply;
        }
    }
}
