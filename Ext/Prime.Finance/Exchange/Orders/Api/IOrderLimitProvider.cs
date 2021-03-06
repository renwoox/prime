using System.Collections.Generic;
using System.Threading.Tasks;
using Prime.Core;

namespace Prime.Finance
{
    public interface IOrderLimitProvider : INetworkProviderPrivate
    {
        Task<PlacedOrderLimitResponse> PlaceOrderLimitAsync(PlaceOrderLimitContext context);

        /// <summary>
        /// Gets the list of all orders. Includes orders from history list and open orders list.
        /// TODO: AY: HH, review this method. Implemented in Bittrex, Poloniex, Binance.
        /// Stopped implementation in other providers because of historical and open orders endpoints differences which make it difficult to merge orders into 1 list.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<TradeOrdersResponse> GetOrdersHistory(TradeOrdersContext context);

        /// <summary>
        /// Gets the list of currently open orders.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<OpenOrdersResponse> GetOpenOrdersAsync(OpenOrdersContext context);
        
        /// <summary>
        /// Gets the status of order with specific id (and market).
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<TradeOrderStatusResponse> GetOrderStatusAsync(RemoteMarketIdContext context);

        /// <summary>
        /// Gets the market where order with specified Remote Id is placed.
        /// </summary>
        /// <param name="context">Remote Id of order.</param>
        /// <returns>Market where specified order was placed.</returns>
        Task<OrderMarketResponse> GetMarketFromOrderAsync(RemoteIdContext context); 

        MinimumTradeVolume[] MinimumTradeVolume { get; }

        OrderLimitFeatures OrderLimitFeatures { get; }
    }
}