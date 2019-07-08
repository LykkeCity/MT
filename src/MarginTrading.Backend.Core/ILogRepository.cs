// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;
using Lykke.Logs;

namespace MarginTrading.Backend.Core
{
    public interface ILogRepository
    {
        Task Insert(ILogEntity log);
    }
}