// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;

namespace MarginTradingTests.Helpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GenericTestCaseAttribute : TestCaseAttribute, ITestBuilder
    {
        private readonly Type _type;

        public GenericTestCaseAttribute(Type type, params object[] arguments) : base(arguments)
        {
            _type = type;
        }

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            if (method.IsGenericMethodDefinition && _type != null)
            {
                var genericMethod = method.MakeGenericMethod(_type);
                return BuildFrom(genericMethod, suite);
            }
            return BuildFrom(method, suite);
        }
    }
}