// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1591

namespace MarginTrading.Backend.Email
{
    public interface ITemplateGenerator
    {
        string Generate<T>(string templateName, T model);
    }
}
