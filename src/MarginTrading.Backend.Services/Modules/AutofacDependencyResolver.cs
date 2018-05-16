using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.Cqrs;

namespace MarginTrading.Backend.Services.Modules
{
    internal class AutofacDependencyResolver : IDependencyResolver
    {
        private readonly IComponentContext _context;

        public AutofacDependencyResolver([NotNull] IComponentContext kernel)
        {
            _context = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        public object GetService(Type type)
        {
            return _context.Resolve(type);
        }

        public bool HasService(Type type)
        {
            return _context.IsRegistered(type);
        }
    }
}