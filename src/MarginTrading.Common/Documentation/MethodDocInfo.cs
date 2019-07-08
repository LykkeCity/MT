// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Common.Documentation
{
    public class MethodDocInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public Type[] InputTypes { get; set; }
        public Type[] OutputTypes { get; set; }
        public string Description { get; set; }
    }
}
