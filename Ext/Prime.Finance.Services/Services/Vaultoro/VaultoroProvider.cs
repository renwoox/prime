﻿using System;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Prime.Base;
using Prime.Core;

namespace Prime.Finance.Services.Services.Vaultoro
{
    /// <author email="scaruana_prime@outlook.com">Sean Caruana</author>
    // https://api.vaultoro.com/#api-Basic_API
    public partial class VaultoroProvider : IPublicPricingProvider, IAssetPairsProvider, IOrderBookProvider, INetworkProviderPrivate
    {
        public Version Version { get; } = new Version(1, 0, 0);
        private const string VaultoroApiUrl = "https://api.vaultoro.com/";
        private const string VaultoroApiUrlPrivate = "https://api.vaultoro.com/" + privateUrlVersion;

        private static readonly ObjectId IdHash = "prime:vaultoro".GetObjectIdHashCode();

        private const string privateUrlVersion = "1";

        //The calls are limited to 1-2 per second. We will be testing different settings when load increases.
        //https://api.vaultoro.com/#api-Basic_API
        private static readonly IRateLimiter Limiter = new PerSecondRateLimiter(2, 1);

        private RestApiClientProvider<IVaultoroApi> ApiProvider { get; }
        private RestApiClientProvider<IVaultoroApi> ApiProviderPrivate { get; }

        public Network Network { get; } = Networks.I.Get("Vaultoro");

        public bool Disabled => false;
        public int Priority => 100;
        public string AggregatorName => null;
        public string Title => Network.Name;
        public ObjectId Id => IdHash;
        public IRateLimiter RateLimiter => Limiter;
        public bool IsDirect => true;
        public char? CommonPairSeparator => '-';

        public ApiConfiguration GetApiConfiguration => ApiConfiguration.Standard2;

        public VaultoroProvider()
        {
            ApiProvider = new RestApiClientProvider<IVaultoroApi>(VaultoroApiUrl, this, (k) => null);
            ApiProviderPrivate = new RestApiClientProvider<IVaultoroApi>(VaultoroApiUrlPrivate, this, (k) => new VaultoroAuthenticator(k).GetRequestModifierAsync);
        }

        public async Task<bool> TestPublicApiAsync(NetworkProviderContext context)
        {
            var api = ApiProvider.GetApi(context);
            var r = await api.GetMarketsAsync().ConfigureAwait(false);

            return r?.status.Equals("success", StringComparison.OrdinalIgnoreCase) == true;
        }

        public async Task<bool> TestPrivateApiAsync(ApiPrivateTestContext context)
        {
            var api = ApiProviderPrivate.GetApi(context);
            var rRaw = await api.GetBalanceAsync().ConfigureAwait(false);

            CheckResponseErrors(rRaw);

            var r = rRaw.GetContent();

            return r != null && r.status.Equals("success");
        }

        public async Task<AssetPairs> GetAssetPairsAsync(NetworkProviderContext context)
        {
            var api = ApiProvider.GetApi(context);

            var r = await api.GetMarketsAsync().ConfigureAwait(false);

            if (!r.status.Equals("success", StringComparison.OrdinalIgnoreCase))
                throw new ApiResponseException("No asset pairs returned", this);

            var pairs = new AssetPairs
            {
                r.data.MarketName.ToAssetPair(this)
            };

            return pairs;
        }

        public IAssetCodeConverter GetAssetCodeConverter()
        {
            return null;
        }

        private static readonly PricingFeatures StaticPricingFeatures = new PricingFeatures()
        {
            Single = new PricingSingleFeatures() { CanStatistics = true, CanVolume = true }
        };

        public PricingFeatures PricingFeatures => StaticPricingFeatures;

        public async Task<MarketPrices> GetPricingAsync(PublicPricesContext context)
        {
            var api = ApiProvider.GetApi(context);
            var r = await api.GetMarketsAsync().ConfigureAwait(false);

            if (!context.Pair.Equals(new AssetPair("BTC", "GLD")))
                throw new AssetPairNotSupportedException(context.Pair, this);

            if (r.status.Equals("success", StringComparison.OrdinalIgnoreCase) == false)
                throw new ApiResponseException("Error obtaining pricing");

            var price = new MarketPrice(Network, context.Pair.Reversed, r.data.LastPrice)
            {
                PriceStatistics = new PriceStatistics(Network, context.Pair.Asset2, null, null, r.data.Low24h, r.data.High24h),
                Volume = new NetworkPairVolume(Network, context.Pair, r.data.Volume24h)
            };

            return new MarketPrices(price.Reversed);
        }

        public async Task<OrderBook> GetOrderBookAsync(OrderBookContext context)
        {
            var api = ApiProvider.GetApi(context);

            var r = await api.GetOrderBookAsync().ConfigureAwait(false);
            var orderBook = new OrderBook(Network, context.Pair.Reversed);

            var maxCount = Math.Min(1000, context.MaxRecordsCount);

            if (!context.Pair.Equals(new AssetPair("BTC", "GLD", this)))
                throw new AssetPairNotSupportedException(context.Pair, this);

            if (!r.status.Equals("success", StringComparison.OrdinalIgnoreCase))
                throw new ApiResponseException("Error obtaining order books");

            VaultoroSchema.OrderBookItemResponse[] arrAsks = null;
            VaultoroSchema.OrderBookItemResponse[] arrBids = null;

            foreach (var entry in r.data)
            {
                if (entry.b != null && entry.b.Length > 0)
                    arrBids = entry.b;

                if (entry.s != null && entry.s.Length > 0)
                    arrAsks = entry.s;
            }

            if (arrAsks == null || arrBids == null)
                throw new ApiResponseException("No order books found");

            foreach (var i in arrBids.Take(maxCount))
                orderBook.AddBid(i.Gold_Price, i.Gold_Amount, true);

            foreach (var i in arrAsks.Take(maxCount))
                orderBook.AddAsk(i.Gold_Price, i.Gold_Amount, true);

            return orderBook.AsPair(context.Pair);
        }
    }
}
