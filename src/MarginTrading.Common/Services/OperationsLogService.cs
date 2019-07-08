// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Common.Services
{
    public interface IOperationsLogService
    {
        void AddLog(string name, string accountId, string input, string data);
    }
}
