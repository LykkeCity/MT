// Copyright (c) 2019 Lykke Corp.

using Microsoft.AspNetCore.Builder;

namespace MarginTrading.Common.Middleware
{
    public class RequestLoggingPipeline
    {
        public void Configure(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<RequestsLoggingMiddleware>();
        }
    }
}
