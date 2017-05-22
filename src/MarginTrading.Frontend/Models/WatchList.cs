using System.Collections.Generic;

namespace MarginTrading.Frontend.Models
{
    public class WatchList
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> AssetIds { get; set; }
    }
}
