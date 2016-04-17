using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using NxtLib;
using NxtWallet.ViewModel.Model;
using Transaction = NxtWallet.ViewModel.Model.Transaction;

namespace NxtWallet
{
    public class FakeNxtServer : ObservableObject, INxtServer
    {
        private bool _isOnline = true;

        public bool IsOnline
        {
            get { return _isOnline; }
            set { Set(ref _isOnline, value); }
        }

        public Task<ulong> GetCurrentBlockId()
        {
            return Task.FromResult(1UL);
        }

        public Task<int> GetBlockHeightAsync(ulong blockId)
        {
            return Task.FromResult(123);
        }

        public Task<long> GetBalanceAsync()
        {
            return Task.FromResult(11 * 100000000L);
        }

        public Task<IEnumerable<Transaction>> GetTransactionsAsync(DateTime lastTimestamp)
        {
            return Task.FromResult(new List<Transaction>().AsEnumerable());
        }

        public Task<IEnumerable<Transaction>> GetTransactionsAsync()
        {
            return GetTransactionsAsync(DateTime.UtcNow);
        }

        public Task<Result<Transaction>> SendMoneyAsync(Account recipient, Amount amount, string message)
        {
            return Task.FromResult(new Result<Transaction>());
        }

        public Task<IEnumerable<Transaction>> GetAssetTradesAsync(DateTime timestamp)
        {
            return Task.FromResult(new List<Transaction>().AsEnumerable());
        }

        public Task<Asset> GetAssetAsync(ulong assetId)
        {
            return Task.FromResult(new Asset());
        }

        public void UpdateNxtServer(string newServerAddress)
        {
        }
    }
}