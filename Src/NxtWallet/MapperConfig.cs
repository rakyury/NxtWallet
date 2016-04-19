﻿using System.Text.RegularExpressions;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NxtLib;
using NxtWallet.Model;
using NxtWallet.ViewModel.Model;
using Transaction = NxtWallet.ViewModel.Model.Transaction;

namespace NxtWallet
{
    public class MapperConfig
    {
        private static MapperConfiguration _configuration;

        public static MapperConfiguration Setup(IWalletRepository repo)
        {
            if (_configuration != null)
                return _configuration;

            var accountRs = repo.NxtAccount.AccountRs;

            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ContactDto, Contact>();
                cfg.CreateMap<Contact, ContactDto>();
                cfg.CreateMap<TransactionDto, Transaction>()
                    .ConstructUsing(GetTransactionObject)
                    .ForMember(dest => dest.NxtId, opt => opt.MapFrom(src => (ulong?)src.NxtId))
                    .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => (TransactionType)src.TransactionType))
                    .AfterMap((src, dest) => dest.UserIsTransactionRecipient = accountRs.Equals(dest.AccountTo))
                    .AfterMap((src, dest) => dest.UserIsTransactionSender = accountRs.Equals(dest.AccountFrom));

                cfg.CreateMap<Transaction, TransactionDto>()
                    .ForMember(dest => dest.NxtId, opt => opt.MapFrom(src => (long?)src.NxtId))
                    .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => (int)src.TransactionType))
                    .AfterMap((src, dest) => dest.Extra = EncodeExtra(src));

                cfg.CreateMap<NxtLib.Transaction, Transaction>()
                    .ConstructUsing(GetTransactionObject)
                    .ForMember(dest => dest.NxtId, opt => opt.MapFrom(src => src.TransactionId ?? 0))
                    .ForMember(dest => dest.Message, opt => opt.MapFrom(src => GetMessage(src)))
                    .ForMember(dest => dest.NqtAmount, opt => opt.MapFrom(src => src.Amount.Nqt))
                    .ForMember(dest => dest.NqtFee, opt => opt.MapFrom(src => src.Fee.Nqt))
                    .ForMember(dest => dest.AccountFrom, opt => opt.MapFrom(src => src.SenderRs))
                    .ForMember(dest => dest.AccountTo, opt => opt.MapFrom(src => src.RecipientRs))
                    .ForMember(dest => dest.IsConfirmed, opt => opt.MapFrom(src => src.Confirmations != null))
                    .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => (TransactionType)(int)src.SubType))
                    .AfterMap((src, dest) => dest.UserIsTransactionRecipient = accountRs.Equals(dest.AccountTo))
                    .AfterMap((src, dest) => dest.UserIsTransactionSender = accountRs.Equals(dest.AccountFrom));

                cfg.CreateMap<NxtLib.AssetExchange.AssetTradeInfo, AssetTradeTransaction>()
                    .ForMember(dest => dest.NxtId, opt => opt.MapFrom(src => src.BuyerRs.Equals(accountRs) ? src.AskOrder : src.BidOrder)) // buyer makes the bidorder
                    .ForMember(dest => dest.Message, opt => opt.UseValue("[Asset Trade]"))
                    .ForMember(dest => dest.NqtAmount, opt => opt.MapFrom(src => (src.BuyerRs.Equals(accountRs) ? -1 : 1) * src.Price.Nqt * src.QuantityQnt))
                    .ForMember(dest => dest.NqtFee, opt => opt.UseValue(Amount.OneNxt.Nqt)) // TODO: Assumption
                    .ForMember(dest => dest.AccountFrom, opt => opt.MapFrom(src => src.BuyerRs.Equals(accountRs) ? src.SellerRs : src.BuyerRs))
                    .ForMember(dest => dest.AccountTo, opt => opt.MapFrom(src => src.SellerRs.Equals(accountRs) ? src.SellerRs : src.BuyerRs))
                    .ForMember(dest => dest.IsConfirmed, opt => opt.UseValue(true))
                    .ForMember(dest => dest.TransactionType, opt => opt.UseValue(TransactionType.AssetTrade))
                    .ForMember(dest => dest.AssetNxtId, opt => opt.MapFrom(src => src.AssetId))
                    .ForMember(dest => dest.QuantityQnt, opt => opt.MapFrom(src => src.QuantityQnt))
                    .AfterMap((src, dest) => dest.UserIsTransactionRecipient = accountRs.Equals(dest.AccountTo))
                    .AfterMap((src, dest) => dest.UserIsTransactionSender = accountRs.Equals(dest.AccountFrom));

                cfg.CreateMap<Asset, AssetDto>();

                cfg.CreateMap<AssetDto, Asset>();

                cfg.CreateMap<NxtLib.AssetExchange.Asset, Asset>()
                    .ForMember(dest => dest.NxtId, opt => opt.MapFrom(src => (long)src.AssetId))
                    .ForMember(dest => dest.Account, opt => opt.MapFrom(src => src.AccountRs));

                cfg.CreateMap<AssetOwnership, AssetOwnershipDto>();
                cfg.CreateMap<AssetOwnershipDto, AssetOwnership>();

            });

            return _configuration;
        }

        private static Transaction GetTransactionObject(TransactionDto transactionDto)
        {
            switch (transactionDto.TransactionType)
            {
                case (int) TransactionType.DigitalGoodsPurchase:
                {
                    var transaction = new DgsPurchaseTransaction();
                    if (!string.IsNullOrEmpty(transactionDto.Extra))
                    {
                        JsonConvert.PopulateObject(transactionDto.Extra, transaction);
                    }
                    return transaction;
                }
                case (int) TransactionType.DigitalGoodsPurchaseExpired:
                {
                    var transaction = new DgsPurchaseExpiredTransaction();
                    if (!string.IsNullOrEmpty(transactionDto.Extra))
                    {
                        JsonConvert.PopulateObject(transactionDto.Extra, transaction);
                    }
                    return transaction;
                }
            }
            return new Transaction();
        }

        private static Transaction GetTransactionObject(NxtLib.Transaction nxtTransaction)
        {
            if (nxtTransaction.SubType == TransactionSubType.DigitalGoodsPurchase)
            {
                var attachment = (DigitalGoodsPurchaseAttachment) nxtTransaction.Attachment;

                return new DgsPurchaseTransaction
                {
                    DeliveryDeadlineTimestamp = attachment.DeliveryDeadlineTimestamp
                };
            }
            return new Transaction();
        }

        private static string EncodeExtra(Transaction transaction)
        {
            switch (transaction.TransactionType)
            {
                case TransactionType.DigitalGoodsPurchase:
                {
                    var purchase = (DgsPurchaseTransaction) transaction;
                    var json = JsonConvert.SerializeObject(purchase, Formatting.None);
                    return json;
                }
                case TransactionType.DigitalGoodsPurchaseExpired:
                {
                    var expired = (DgsPurchaseExpiredTransaction) transaction;
                    var json = JsonConvert.SerializeObject(expired, Formatting.None);
                    return json;
                }
            }
            return string.Empty;
        }

        private static string GetMessage(NxtLib.Transaction transaction)
        {
            if (transaction.SubType == TransactionSubType.PaymentOrdinaryPayment ||
                transaction.SubType == TransactionSubType.MessagingArbitraryMessage)
            {
                return transaction.Message?.MessageText;
            }
            var input = "[" + (TransactionType)(int)transaction.SubType + "]";
            return Regex.Replace(input, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }
    }
}
