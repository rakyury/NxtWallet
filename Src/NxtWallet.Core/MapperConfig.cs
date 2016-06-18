﻿using System.Text.RegularExpressions;
using AutoMapper;
using Newtonsoft.Json;
using NxtLib;
using NxtWallet.Core.Model;
using NxtWallet.Core.ViewModel.Model;
using NxtWallet.Core.Migrations.Model;
using Transaction = NxtWallet.Core.ViewModel.Model.Transaction;
using System.Numerics;
using System.Linq;

namespace NxtWallet.Core
{
    public class MapperConfig
    {
        private static MapperConfiguration _configuration;
        private static string _accountRs;

        public static MapperConfiguration Setup(IWalletRepository repo)
        {
            if (_configuration != null)
                return _configuration;

            _accountRs = repo.NxtAccount.AccountRs;

            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ContactDto, Contact>();
                cfg.CreateMap<Contact, ContactDto>();
                cfg.CreateMap<TransactionDto, Transaction>()
                    .ConstructUsing(GetTransactionObject)
                    .ForMember(dest => dest.NxtId, opt => opt.MapFrom(src => (ulong?)src.NxtId))
                    .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => (TransactionType)src.TransactionType))
                    .AfterMap((src, dest) => dest.UserIsTransactionRecipient = _accountRs.Equals(dest.AccountTo))
                    .AfterMap((src, dest) => dest.UserIsTransactionSender = _accountRs.Equals(dest.AccountFrom));

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
                    .AfterMap((src, dest) => dest.UserIsTransactionRecipient = _accountRs.Equals(dest.AccountTo))
                    .AfterMap((src, dest) => dest.UserIsTransactionSender = _accountRs.Equals(dest.AccountFrom));

                cfg.CreateMap<NxtLib.AssetExchange.AssetTradeInfo, AssetTradeTransaction>()
                    .ForMember(dest => dest.NxtId, opt => opt.MapFrom(src => src.BuyerRs.Equals(_accountRs) ? src.AskOrder : src.BidOrder)) // buyer makes the bidorder
                    .ForMember(dest => dest.Message, opt => opt.UseValue("[Asset Trade]"))
                    .ForMember(dest => dest.NqtAmount, opt => opt.MapFrom(src => (src.BuyerRs.Equals(_accountRs) ? -1 : 1) * src.Price.Nqt * src.QuantityQnt))
                    .ForMember(dest => dest.NqtFee, opt => opt.UseValue(Amount.OneNxt.Nqt)) // TODO: Assumption
                    .ForMember(dest => dest.AccountFrom, opt => opt.MapFrom(src => src.BuyerRs.Equals(_accountRs) ? src.SellerRs : src.BuyerRs))
                    .ForMember(dest => dest.AccountTo, opt => opt.MapFrom(src => src.SellerRs.Equals(_accountRs) ? src.SellerRs : src.BuyerRs))
                    .ForMember(dest => dest.IsConfirmed, opt => opt.UseValue(true))
                    .ForMember(dest => dest.TransactionType, opt => opt.UseValue(TransactionType.AssetTrade))
                    .ForMember(dest => dest.AssetNxtId, opt => opt.MapFrom(src => src.AssetId))
                    .ForMember(dest => dest.QuantityQnt, opt => opt.MapFrom(src => src.QuantityQnt))
                    .AfterMap((src, dest) => dest.UserIsTransactionRecipient = _accountRs.Equals(dest.AccountTo))
                    .AfterMap((src, dest) => dest.UserIsTransactionSender = _accountRs.Equals(dest.AccountFrom));

                cfg.CreateMap<NxtLib.MonetarySystem.CurrencyExchange, MsCurrencyExchangeTransaction>()
                    .ForMember(dest => dest.Message, opt => opt.UseValue("[Currency Exchange]"))
                    .ForMember(dest => dest.OfferNxtId, opt => opt.MapFrom(src => (long) src.OfferId))
                    .ForMember(dest => dest.TransactionNxtId, opt => opt.MapFrom(src => (long) src.TransactionId))
                    .ForMember(dest => dest.Units, opt => opt.MapFrom(src => src.Units))
                    .ForMember(dest => dest.NqtFee, opt => opt.UseValue(0))
                    .ForMember(dest => dest.AccountFrom, opt => opt.MapFrom(src => src.BuyerRs))
                    .ForMember(dest => dest.AccountTo, opt => opt.MapFrom(src => src.SellerRs))
                    .ForMember(dest => dest.NqtAmount, opt => opt.MapFrom(src => src.Units*src.Rate.Nqt))
                    .ForMember(dest => dest.IsConfirmed, opt => opt.UseValue(true))
                    .ForMember(dest => dest.TransactionType, opt => opt.UseValue(TransactionType.CurrencyExchange))
                    .AfterMap((src, dest) => dest.UserIsTransactionRecipient = _accountRs.Equals(dest.AccountTo))
                    .AfterMap((src, dest) => dest.UserIsTransactionSender = _accountRs.Equals(dest.AccountFrom));

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
                case (int) TransactionType.AssetTrade:
                {
                    return PopulateObject(new AssetTradeTransaction(), transactionDto);
                }
                case (int) TransactionType.DigitalGoodsPurchase:
                {
                    return PopulateObject(new DgsPurchaseTransaction(), transactionDto);
                }
                case (int)TransactionType.ReserveIncrease:
                {
                    return PopulateObject(new MsReserveIncreaseTransaction(), transactionDto);
                }
                case (int)TransactionType.PublishExchangeOffer:
                {
                    return PopulateObject(new MsPublishExchangeOfferTransaction(), transactionDto);
                }
                case (int)TransactionType.ShufflingCreation:
                {
                    return PopulateObject(new ShufflingCreationTransaction(), transactionDto);
                }
                case (int)TransactionType.ShufflingRegistration:
                {
                    return PopulateObject(new ShufflingRegistrationTransaction(), transactionDto);
                }
                case (int)TransactionType.DigitalGoodsPurchaseExpired:
                {
                    return PopulateObject(new DgsPurchaseExpiredTransaction(), transactionDto);
                }
                case (int)TransactionType.CurrencyUndoCrowdfunding:
                {
                    return PopulateObject(new MsUndoCrowdfundingTransaction(), transactionDto);
                }
                case (int)TransactionType.CurrencyExchange:
                {
                    return PopulateObject(new MsCurrencyExchangeTransaction(), transactionDto);
                }
                case (int)TransactionType.CurrencyOfferExpired:
                {
                    return PopulateObject(new MsExchangeOfferExpiredTransaction(), transactionDto);
                }
                case (int)TransactionType.ShufflingRefund:
                {
                    return PopulateObject(new ShufflingRefundTransaction(), transactionDto);
                }
                case (int)TransactionType.ShufflingDistribution:
                {
                    return PopulateObject(new ShufflingDistributionTransaction(), transactionDto);
                }
            }
            return new Transaction();
        }

        private static Transaction PopulateObject(Transaction transaction, TransactionDto transactionDto)
        {
            if (!string.IsNullOrEmpty(transactionDto.Extra))
            {
                JsonConvert.PopulateObject(transactionDto.Extra, transaction);
            }
            return transaction;
        }

        private static Transaction GetTransactionObject(NxtLib.Transaction nxtTransaction)
        {
            switch (nxtTransaction.SubType)
            {
                case TransactionSubType.DigitalGoodsPurchase:
                {
                    var attachment = (DigitalGoodsPurchaseAttachment) nxtTransaction.Attachment;

                    if (nxtTransaction.SenderRs == _accountRs)
                    {
                        nxtTransaction.Amount = Amount.CreateAmountFromNqt(nxtTransaction.Amount.Nqt + (attachment.Price.Nqt * attachment.Quantity));
                    }

                    return new DgsPurchaseTransaction
                    {
                        DeliveryDeadlineTimestamp = attachment.DeliveryDeadlineTimestamp
                    };
                }
                case TransactionSubType.DigitalGoodsRefund:
                {
                    var attachment = (DigitalGoodsRefundAttachment)nxtTransaction.Attachment;
                    nxtTransaction.Amount = Amount.CreateAmountFromNqt(nxtTransaction.Amount.Nqt + attachment.Refund.Nqt);
                    break;
                }
                case TransactionSubType.MonetarySystemReserveIncrease:
                {
                    var attachment = (MonetarySystemReserveIncreaseAttachment) nxtTransaction.Attachment;

                    return new MsReserveIncreaseTransaction
                    {
                        CurrencyId = (long) attachment.CurrencyId
                    };
                }
                case TransactionSubType.MonetarySystemPublishExchangeOffer:
                {
                    var attachment = (MonetarySystemPublishExchangeOfferAttachment) nxtTransaction.Attachment;
                    nxtTransaction.Amount = Amount.CreateAmountFromNqt(attachment.BuyRate.Nqt * attachment.InitialBuySupply);

                    return new MsPublishExchangeOfferTransaction
                    {
                        CurrencyId = (long)attachment.CurrencyId,
                        ExpirationHeight = attachment.ExpirationHeight,
                        IsExpired = false,
                        BuyLimit = attachment.TotalBuyLimit,
                        BuySupply = attachment.InitialBuySupply,
                        BuyRateNqt = attachment.BuyRate.Nqt,
                        SellLimit = attachment.TotalSellLimit,
                        SellSupply = attachment.InitialSellSupply,
                        SellRateNqt = attachment.SellRate.Nqt
                    };
                }
                case TransactionSubType.ShufflingCreation:
                {
                    var attachment = (ShufflingCreationAttachment)nxtTransaction.Attachment;
                    if (attachment.HoldingType == HoldingType.Nxt)
                    {
                        nxtTransaction.Amount = attachment.Amount;
                    }
                    
                    return new ShufflingCreationTransaction
                    {
                        RegistrationPeriod = attachment.HoldingType == HoldingType.Nxt ? attachment.RegistrationPeriod : 0
                    };
                }
                case TransactionSubType.ShufflingRegistration:
                {
                    var attachment = (ShufflingRegistrationAttachment)nxtTransaction.Attachment;
                    var idBytes = attachment.ShufflingFullHash.ToBytes().ToArray();
                    var bigInteger = new BigInteger(idBytes.Take(8).ToArray());
                    var shufflingId = (long)bigInteger;

                    return new ShufflingRegistrationTransaction
                    {
                        ShufflingId = shufflingId
                    };
                }
            }
            return new Transaction();
        }

        private static string EncodeExtra(Transaction transaction)
        {
            if (transaction.TransactionType == TransactionType.DigitalGoodsPurchase ||
                transaction.TransactionType == TransactionType.DigitalGoodsPurchaseExpired ||
                transaction.TransactionType == TransactionType.ReserveIncrease ||
                transaction.TransactionType == TransactionType.CurrencyUndoCrowdfunding ||
                transaction.TransactionType == TransactionType.CurrencyExchange ||
                transaction.TransactionType == TransactionType.CurrencyOfferExpired ||
                transaction.TransactionType == TransactionType.PublishExchangeOffer ||
                transaction.TransactionType == TransactionType.ShufflingCreation ||
                transaction.TransactionType == TransactionType.ShufflingRegistration ||
                transaction.TransactionType == TransactionType.ShufflingRefund ||
                transaction.TransactionType == TransactionType.ShufflingDistribution)
            {
                var json = JsonConvert.SerializeObject(transaction, Formatting.None);
                return json;
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