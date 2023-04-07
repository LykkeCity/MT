// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public interface IConfigurationValidator
    {
        Task WarnIfInvalidAsync();
    }
}