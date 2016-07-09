﻿using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using NxtWallet.ViewModel;

namespace NxtWallet.Views
{
    public sealed partial class OverviewPage
    {
        private OverviewViewModel ViewModel => (OverviewViewModel) DataContext;

        public OverviewPage()
        {
            InitializeComponent();
            TransactionList.SelectedLedgerEntryChanged += OnSelectedTransactionChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.LoadFromRepository();
            Bindings.Update();
        }

        private void OnSelectedTransactionChanged(object source, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            if (selectionChangedEventArgs.AddedItems.Any())
            {
                Frame.Navigate(typeof(TransactionDetailPage), selectionChangedEventArgs.AddedItems.Single());
            }
        }
    }
}
