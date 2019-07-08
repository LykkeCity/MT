// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Services
{
    public interface ISpecialLiquidationService
    {
        void FakeGetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume);
    }
}