using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class SimpleIdentityGenerator : IIdentityGenerator
    {
        private long _currentId;
        private readonly Random _random = new Random();
        private const string Pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly object _lockObject = new object();
        
        public SimpleIdentityGenerator()
        {
            _currentId = DateTime.Now.Ticks;
        }
        
        public Task<long> GenerateIdAsync(string entityType)
        {
            _currentId++;
            return Task.FromResult(_currentId);
        }

        public string GenerateAlphanumericId()
        {
            lock(_lockObject)
            {
                var chars = Enumerable.Range(0, 10).Select(x => Pool[_random.Next(0, Pool.Length)]);
                return new string(chars.ToArray());
            }
        }

        public string GenerateGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}