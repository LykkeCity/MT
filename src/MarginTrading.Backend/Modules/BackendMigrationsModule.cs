// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Autofac;
using MarginTrading.Backend.Services.Migrations;
using Module = Autofac.Module;

namespace MarginTrading.Backend.Modules
{
    public class BackendMigrationsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var migrationTypes = Assembly.GetAssembly(typeof(IMigration)).GetTypes()
                .Where(myType => myType.IsClass && myType.GetInterfaces().Contains(typeof(IMigration)))
                .ToList();
            
            foreach (var type in migrationTypes)
            {
                builder.RegisterType(type).As<IMigration>().SingleInstance();
            }
        }
    }
}