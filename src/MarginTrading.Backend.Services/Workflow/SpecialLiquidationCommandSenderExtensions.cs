// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Cqrs;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;

namespace MarginTrading.Backend.Services.Workflow
{
    public static class SpecialLiquidationCommandSenderExtensions
    {
        /// <summary>
        /// Sends a command to resume the initial flow (liquidation)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="liquidationId"></param>
        /// <param name="specialLiquidationId"></param>
        /// <param name="timestamp"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SendResumeLiquidation(this ICommandSender sender,
            string liquidationId,
            string specialLiquidationId,
            DateTime timestamp)
        {
            if (string.IsNullOrWhiteSpace(liquidationId))
                throw new ArgumentNullException(nameof(liquidationId));
            
            if (string.IsNullOrWhiteSpace(specialLiquidationId))
                throw new ArgumentNullException(nameof(specialLiquidationId));
            
            sender.SendCommand(new ResumeLiquidationInternalCommand
            {
                OperationId = liquidationId,
                CreationTime = timestamp,
                Comment = $"Resume after special liquidation {specialLiquidationId} failed.",
                IsCausedBySpecialLiquidation = true,
                CausationOperationId = specialLiquidationId
            }, "TradingEngine");
        }

        /// <summary>
        /// Sends a command to cancel special liquidation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="liquidationId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SendCancellation(this ICommandSender sender, string liquidationId)
        {
            if (string.IsNullOrWhiteSpace(liquidationId))
                throw new ArgumentNullException(nameof(liquidationId));
            
            sender.SendCommand(new CancelSpecialLiquidationCommand
            {
                OperationId = liquidationId,
                Reason = "Liquidity is enough to close positions within regular flow"
            }, "TradingEngine");
        }
    }
}