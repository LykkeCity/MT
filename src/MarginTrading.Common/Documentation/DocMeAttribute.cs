using System;

namespace MarginTrading.Common.Documentation
{
    public class DocMeAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type InputType { get; set; }
    }
}
