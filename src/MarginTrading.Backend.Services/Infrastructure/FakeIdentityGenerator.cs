using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class FakeIdentityGenerator : IIdentityGenerator
    {
        private long _currentId;
        
        public FakeIdentityGenerator()
        {
            _currentId = DateTime.Now.Ticks;
        }
        
        public Task<long> GenerateIdAsync(string entityType)
        {
            _currentId++;
            return Task.FromResult(_currentId);
        }
    }
}