﻿using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NxtWallet.Controls;
using NxtWallet.Model;

namespace NxtWallet.ViewModel
{
    public class SendMoneyViewModel : ViewModelBase
    {
        private readonly INxtServer _nxtServer;
        private readonly IWalletRepository _walletRepository;
        private readonly ISendMoneyDialog _sendMoneyDialog;

        private string _recipient;
        private string _amount;
        private string _message;

        public string Recipient
        {
            get { return _recipient; }
            set { Set(ref _recipient, value); }
        }

        public string Amount
        {
            get { return _amount; }
            set { Set(ref _amount, value); }
        }

        public string Message
        {
            get { return _message; }
            set { Set(ref _message, value); }
        }

        public RelayCommand SendMoneyCommand { get; }

        public SendMoneyViewModel(INxtServer nxtServer, IWalletRepository walletRepository, ISendMoneyDialog sendMoneyDialog)
        {
            _nxtServer = nxtServer;
            _walletRepository = walletRepository;
            _sendMoneyDialog = sendMoneyDialog;
            SendMoneyCommand = new RelayCommand(SendMoney);
            nxtServer.PropertyChanged += (sender, args) => SendMoneyCommand.CanExecute(_nxtServer.IsOnline);
        }

        private async void SendMoney()
        {
            // ReSharper disable once UnusedVariable
            var ignore = _sendMoneyDialog.ShowAsync();
            await Task.Run(async () =>
            {
                decimal amount;
                decimal.TryParse(Amount, out amount);
                var transaction = await _nxtServer.SendMoneyAsync(Recipient, NxtLib.Amount.CreateAmountFromNxt(amount), Message);
                await _walletRepository.SaveTransactionAsync(transaction);
                var balance = CalculateNewBalance(transaction);
                await _walletRepository.SaveBalanceAsync(balance);
                //await Task.Delay(5000); // For testing purposes
            });
            _sendMoneyDialog.Hide();
        }

        private string CalculateNewBalance(ITransaction transaction)
        {
            // TODO: Could be a problem with different decimal separator signs in different regions
            var currentBalanceNxt = decimal.Parse(_walletRepository.Balance);
            var currentBalanceNqt = currentBalanceNxt*100000000;
            var newBalanceNqt = currentBalanceNqt - transaction.NqtAmount - transaction.NqtFeeAmount;
            var newBalanceNxt = newBalanceNqt/100000000M;
            return newBalanceNxt.ToFormattedString();
        }
    }
}