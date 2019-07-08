// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
