using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Core
{
    public interface IAsset
    {
        string Id { get; }
        string Name { get; }
        int Accuracy { get; }
    }

    public class Asset : IAsset
    {
        public string Id { get; }
        public string Name { get; }
        public int Accuracy { get;  }

        public Asset(string id, string name, int accuracy)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Accuracy = accuracy;

         }
    }
}