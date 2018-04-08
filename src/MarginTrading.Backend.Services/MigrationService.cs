using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Migrations;

namespace MarginTrading.Backend.Services
{
    public class MigrationService : IMigrationService
    {
        private readonly IReadOnlyList<Type> _migrationTypes;
        
        public MigrationService()
        {
            _migrationTypes = Assembly.GetAssembly(typeof(AbstractMigration)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(AbstractMigration)))
                .ToList();
        }
        
        public async Task InvokeAll()
        {
            foreach (var migrationType in _migrationTypes)
            {
                var instance = (AbstractMigration)Activator.CreateInstance(migrationType, null);
                await instance.Invoke();
            }
        }
    }
}