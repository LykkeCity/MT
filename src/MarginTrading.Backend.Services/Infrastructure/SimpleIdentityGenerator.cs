// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class SimpleIdentityGenerator : IIdentityGenerator
    {
        private long _currentId;
        
        public SimpleIdentityGenerator()
        {
            _currentId = DateTime.Now.Ticks;
        }
        
        public Task<long> GenerateIdAsync(string entityType)
        {
            _currentId++;
            return Task.FromResult(_currentId);
        }

        public string GenerateGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}