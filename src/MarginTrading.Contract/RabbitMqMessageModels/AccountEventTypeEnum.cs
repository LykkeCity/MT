// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.RabbitMqMessageModels
{
    /// <summary>
    /// What happend to the account
    /// </summary>
    public enum AccountEventTypeEnum
    {
        Created = 1,
        Updated = 2,
        Deleted = 3,
    }
}