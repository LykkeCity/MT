// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lykke.Logs;

namespace MarginTrading.Backend.Core
{
    public interface ILogRepository
    {
        Task Insert(ILogEntity log);
    }
}