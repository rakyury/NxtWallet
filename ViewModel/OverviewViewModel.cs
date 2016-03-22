﻿using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace NxtWallet.ViewModel
{
    public class OverviewViewModel : ViewModelBase
    {
        private readonly IWalletRepository _walletRepository;
        private readonly INxtServer _nxtServer;

        private string _balance;

        public string NxtAddress { get; set; }

        public string Balance
        {
            get { return _balance; }
            set { Set(ref _balance, value); }
        }

        public OverviewViewModel(IWalletRepository walletRepository, INxtServer nxtServer)
        {
            _walletRepository = walletRepository;
            _nxtServer = nxtServer;

            Balance = "0.0";
            NxtAddress = walletRepository.NxtAccount.AccountRs;
        }

        public void LoadFromRepository()
        {
            Balance = _walletRepository.Balance;
        }

        public async Task LoadFromNxtServerAsync()
        {
            var balanceResult = await _nxtServer.GetBalanceAsync();
            if (balanceResult.Success)
            {
                Balance = balanceResult.Value;
                await _walletRepository.SaveBalanceAsync(Balance);
            }
        }
    }
}