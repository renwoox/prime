﻿using System;
using Prime.Core;
using Prime.Core.Exchange.Rates;
using Prime.Finance.Prices.Latest;

namespace Prime.Finance
{
    internal sealed class LatestPriceProviderContext
    {
        internal LatestPriceProviderContext(IPublicPricingProvider provider, Aggregator aggregator)
        {
            Provider = provider;
            Aggregator = aggregator;
        }

        internal TimeSpan PollingSpan { get; set; } = new TimeSpan(0, 0, 15);
        internal IPublicPricingProvider Provider { get; private set; }
        internal Aggregator Aggregator { get; private set; }
    }
}