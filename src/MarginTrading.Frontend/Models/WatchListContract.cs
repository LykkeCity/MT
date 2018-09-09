using System.Collections.Generic;

namespace MarginTrading.Frontend.Models
{
    public class WatchListContract
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool ReadOnly { get; set; }
        public int Order { get; set; }
        public List<string> AssetIds { get; set; }
    }
}