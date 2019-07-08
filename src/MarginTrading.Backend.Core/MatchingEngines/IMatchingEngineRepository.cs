// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineRepository
    {
        IMatchingEngineBase GetMatchingEngineById(string id);
        ICollection<IMatchingEngineBase> GetMatchingEngines();
    }
}
