// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Backend.Core.Snapshots
{
    /// <summary>
    /// Represent result of trading state validation for <see cref="T"/> entity. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValidationResult<T>
    {
        /// <summary>
        /// Indicates the validity of current state.
        /// </summary>
        public bool IsValid => !Extra.Any() && !Missed.Any() && !Inconsistent.Any();

        /// <summary>
        /// The collection of extra entities in the current state. 
        /// </summary>
        public IReadOnlyList<T> Extra { get; set; }

        /// <summary>
        /// The collection of missed entities in the current state. 
        /// </summary>
        public IReadOnlyList<T> Missed { get; set; }

        /// <summary>
        /// The collection of inconsistent entities in the current state. 
        /// </summary>
        public IReadOnlyList<T> Inconsistent { get; set; }
    }
}