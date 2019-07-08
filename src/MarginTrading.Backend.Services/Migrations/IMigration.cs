// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Migrations
{
    public interface IMigration
    {
        int Version { get; }

        Task Invoke();
    }
}