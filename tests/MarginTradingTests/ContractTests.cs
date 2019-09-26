using System;
using System.Linq;
using MoreLinq;
using NUnit.Framework;

namespace MarginTradingTests
{
    public class ContractTests
    {
        private const string GlobalAssembly = "MarginTrading";
        private const string ClientAssembly = "MarginTrading.Backend.Contracts";
        private const string Contract = "Contract";
        
        //TODO multiple issues with contracts, needs to be fixed first.
//        [Test]
//        public void Verify_EnumContracts_HaveTheSameBodyAsDomain()
//        {
//            var allEnumContracts = AppDomain.CurrentDomain.GetAssemblies()
//                .Single(assembly => assembly.GetName().Name == ClientAssembly)
//                .GetTypes()
//                .Where(x => x.IsSubclassOf(typeof(Enum)) && x.Name.EndsWith(Contract))
//                .ToDictionary(x => x.Name);
//            var allDomainEnums = AppDomain.CurrentDomain.GetAssemblies()
//                .Where(assembly => assembly.GetName().Name.StartsWith(GlobalAssembly) 
//                                   && assembly.GetName().Name != ClientAssembly)
//                .SelectMany(x => x.GetTypes())
//                .Where(x => x.IsSubclassOf(typeof(Enum)) && !x.Name.EndsWith(Contract))
//                .ToDictionary(x => x.Name);
//
//            foreach (var (key, value) in allEnumContracts)
//            {
//                if (!allDomainEnums.TryGetValue(key
//                    .Substring(0, key.Length - Contract.Length), out var domainEnum))
//                {
//                    throw new Exception($"{key} has no domain implementation");
//                }
//
//                if (!Enum.GetValues(value).Cast<int>().ToList()
//                        .SequenceEqual(Enum.GetValues(domainEnum).Cast<int>().ToList())
//                    || !Enum.GetNames(value).SequenceEqual(Enum.GetNames(domainEnum)))
//                {
//                    throw new Exception($"Contract {value.Name} differs from domain representation {domainEnum.Name}");
//                }
//            }
//        }
    }
}