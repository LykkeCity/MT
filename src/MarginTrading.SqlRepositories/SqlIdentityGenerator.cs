// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.SqlRepositories
{
    public class SqlIdentityGenerator : IIdentityGenerator
    {
        public SqlIdentityGenerator()
        {
        }

        public Task<long> GenerateIdAsync(string entityType)
        {
            return Task.FromResult<long>(0);
//            long id = 0;
//            var result =
//                await
//                    _tableStorage.InsertOrModifyAsync(IdentityEntity.GeneratePartitionKey(entityType),
//                        IdentityEntity.GenerateRowKey,
//                        () => IdentityEntity.Create(entityType),
//                        itm =>
//                        {
//                            itm.Value++;
//                            id = itm.Value;
//                            return true;
//                        }
//                    );
//
//
//            if (!result)
//                throw new InvalidOperationException("Error generating ID");
//
//            return id;
        }

        public string GenerateAlphanumericId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string GenerateGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}