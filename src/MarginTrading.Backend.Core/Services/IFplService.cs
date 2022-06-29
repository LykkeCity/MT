// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface IFplService
    {
        void UpdatePositionFpl(Position position);
        
        decimal GetInitMarginForOrder(Order order, decimal actualVolume);

        decimal CalculateOvernightMaintenanceMargin(Position position);
    }
}
