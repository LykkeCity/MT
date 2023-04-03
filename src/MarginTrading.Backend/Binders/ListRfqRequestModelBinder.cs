// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
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
                .GetValue(nameof(ListRfqRequest.RfqId))
                .FirstValue;
            
            model.InstrumentId = bindingContext.ValueProvider
                .GetValue(nameof(ListRfqRequest.InstrumentId))
                .FirstValue;
            
            model.AccountId = bindingContext.ValueProvider
                .GetValue(nameof(ListRfqRequest.AccountId))
                .FirstValue;

            if (bindingContext.ValueProvider
                .GetValue(nameof(ListRfqRequest.States))
                .TryParseAsEnumList<RfqOperationState>(out var states))
            {
                model.States = states.Distinct().ToList();
            }
            
            var dateFromValue = bindingContext.ValueProvider.GetValue(nameof(ListRfqRequest.DateFrom)).FirstValue;
            if(DateTime.TryParse(dateFromValue, out var dateFrom))
            {
                model.DateFrom = dateFrom;
            }

            var dateToValue = bindingContext.ValueProvider.GetValue(nameof(ListRfqRequest.DateTo)).FirstValue;
            if(DateTime.TryParse(dateToValue, out var dateTo))
            {
                model.DateTo = dateTo;
            }
            
            if (bool.TryParse(bindingContext.ValueProvider
                    .GetValue(nameof(ListRfqRequest.CanBePaused))
                    .FirstValue, 
                    out var canBePaused))
            {
                model.CanBePaused = canBePaused;
            }
            
            if (bool.TryParse(bindingContext.ValueProvider
                        .GetValue(nameof(ListRfqRequest.CanBeResumed))
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