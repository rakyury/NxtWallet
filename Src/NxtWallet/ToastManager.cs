﻿using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using NxtWallet.Core;
using NxtWallet.Core.Repositories;
using NxtWallet.Core.Models;

namespace NxtWallet
{
    public interface IToastManager
    {
        void Register();
    }

    public class ToastManager : IToastManager
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IAccountLedgerRunner _accountLedgerRunner;

        public ToastManager(IWalletRepository walletRepository, IAccountLedgerRunner accountLedgerRunner)
        {
            _walletRepository = walletRepository;
            _accountLedgerRunner = accountLedgerRunner;
        }

        public void Register()
        {
            _accountLedgerRunner.LedgerEntryAdded += (sender, ledgerEntry) =>
            {
                if (ledgerEntry.UserIsRecipient && _walletRepository.NotificationsEnabled)
                    PopNewLedgerEntryToast(ledgerEntry);
            };
        }

        private static void PopNewLedgerEntryToast(LedgerEntry ledgerEntry)
        {
            var message = string.IsNullOrEmpty(ledgerEntry.Message) ? string.Empty : $"\nMessage: {ledgerEntry.Message}";
            var from = ledgerEntry.ContactListAccountFrom ?? ledgerEntry.AccountFrom;

            var xmlToast =  "<toast launch=\"app-defined-string\">" +
                                "<visual>" +
                                "<binding template =\"ToastGeneric\">" +
                                    "<text>New NXT ledger entry</text>" +
                                    "<text>" +
                                    $"You received {ledgerEntry.NxtAmount.ToString("#,##0.00;;")} NXT from {from}.\n" + 
                                    $"Your new balance is {ledgerEntry.NxtBalance.ToString("#,##0.00;;")} NXT." +
                                    $"{message}" + 
                                    "</text>" +
                                "</binding>" +
                                "</visual>" +
                            "</toast>";

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlToast);
            var toastNotification = new ToastNotification(xmlDocument);
            var toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(toastNotification);
        }
    }
}
