// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;

namespace MarginTrading.Backend.Core
{
    public static class ContainerProvider
    {
        public static ILifetimeScope LifetimeScope { get; set; }
    }
}