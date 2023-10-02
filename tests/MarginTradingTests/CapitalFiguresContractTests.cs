// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using AccountManagementModel = 
    MarginTrading.AccountsManagement.Contracts.Api.GetDisposableCapitalRequest.AccountCapitalFigures;
using OwnModel = MarginTrading.Backend.Contracts.Account.AccountCapitalFigures;

namespace MarginTradingTests
{
    /// <summary>
    /// The idea of this test is to match properties of CapitalFigures model which is
    /// being sent as a response and model which is being sent as a request to Accounts Management API.
    /// This requirement appeared in a try to optimize the "conversations" between MT Core and AM.
    /// In particular, there is a scenario (Funds withdrawal) when Trading core needs to know available
    /// disposable amount. This value is calculated by AM service but to calculate it AM needs to know
    /// capital figures information which is kept in MT Core.
    /// Accounts Management API was extended to optionally include capital figures information so that
    /// when MT Core asks for disposable capital it can also send capital figures information. Otherwise,
    /// AM would have to make an additional request to MT Core to get that capital figures information.
    /// In those circumstances it is important to ensure that capital figures contract is the same for
    /// request and response. Any deviation would lead to runtime errors and/or incorrect calculations.
    /// </summary>
    public sealed class CapitalFiguresContractTests
    {
        private const BindingFlags BindingFlags =
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

        [Test]
        public void CapitalFigures_ShouldBeSame_ForRequest_And_ForResponse()
        {
            var amModelProps = typeof(AccountManagementModel).GetProperties(BindingFlags);
            var ownModelProps = typeof(OwnModel).GetProperties(BindingFlags);
            
            Assert.AreEqual(amModelProps.Length, ownModelProps.Length, "Different number of properties");
            
            foreach (var ownModelProp in ownModelProps)
            {
                var correspondingAmModelProp = amModelProps.SingleOrDefault(p =>
                    p.Name == ownModelProp.Name && p.PropertyType == ownModelProp.PropertyType);
                
                Assert.NotNull(correspondingAmModelProp, "Property not found: " + ownModelProp.Name);
            }
        }
    }
}