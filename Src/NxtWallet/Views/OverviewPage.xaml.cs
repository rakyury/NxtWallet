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
            AccountLedgerList.SelectedLedgerEntryChanged += OnSelectedLedgerEntryChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.LoadFromRepository();
            Bindings.Update();
        }

        private void OnSelectedLedgerEntryChanged(object source, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            if (selectionChangedEventArgs.AddedItems.Any())
            {
                Frame.Navigate(typeof(LedgerEntryDetailPage), selectionChangedEventArgs.AddedItems.Single());
            }
        }
    }
}
