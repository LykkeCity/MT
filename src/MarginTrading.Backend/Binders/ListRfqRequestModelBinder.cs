// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Lykke.Snow.Common.Extensions;
using MarginTrading.Backend.Contracts.Rfq;
using MarginTrading.Backend.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MarginTrading.Backend.Binders
{
    internal class ListRfqRequestModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var model = new ListRfqRequest();
            
            model.RfqId = bindingContext.ValueProvider
                .GetValue("rfqId")
                .FirstValue;
            
            model.InstrumentId = bindingContext.ValueProvider
                .GetValue("instrumentId")
                .FirstValue;
            
            model.AccountId = bindingContext.ValueProvider
                .GetValue("accountId")
                .FirstValue;

            if (bindingContext.ValueProvider
                .GetValue("states")
                .TryParseAsEnumList<RfqOperationState>(out var states))
            {
                model.States = states.Distinct().ToList();
            }

            if (bindingContext.ValueProvider
                .GetValue("dateFrom")
                .FirstValue
                .TryParseAsUtcDate(out var dateFrom))
            {
                model.DateFrom = dateFrom;
            }
            
            if (bindingContext.ValueProvider
                .GetValue("dateTo")
                .FirstValue
                .TryParseAsUtcDate(out var dateTo))
            {
                model.DateTo = dateTo;
            }
            
            if (bool.TryParse(bindingContext.ValueProvider
                    .GetValue("canBePaused")
                    .FirstValue, 
                    out var canBePaused))
            {
                model.CanBePaused = canBePaused;
            }
            
            if (bool.TryParse(bindingContext.ValueProvider
                        .GetValue("canBeResumed")
                        .FirstValue, 
                    out var canBeResumed))
            {
                model.CanBeResumed = canBeResumed;
            }
            
            bindingContext.Result = ModelBindingResult.Success(model);
            return Task.CompletedTask;
        }
    }
}