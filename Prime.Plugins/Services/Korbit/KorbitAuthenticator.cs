﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prime.Common;

namespace Prime.Plugins.Services.Korbit
{
    internal class KorbitAuthenticator : BaseAuthenticator
    {
        public KorbitAuthenticator(ApiKey apiKey) : base(apiKey)
        {
        }

        public override void RequestModifyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            
        }
    }
}
