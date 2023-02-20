// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.Rfq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MarginTrading.Backend.Binders
{
    internal class CoreModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(ListRfqRequest))
            {
                return new ListRfqRequestModelBinder();
            }

            return null;
        }
    }
}