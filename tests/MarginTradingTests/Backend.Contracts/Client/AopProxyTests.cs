using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MarginTrading.Backend.Contracts.Infrastructure;
using Newtonsoft.Json;
using NUnit.Framework;

namespace MarginTradingTests.Backend.Contracts.Client
{
    public class AopProxyTests
    {
        [Test]
        public async Task Always_ShouldInvokeMethodsCorrectly()
        {
            // arrange
            var performedActions = new List<string>();
            var proxy = AopProxy.Create<ITest>(new Test(performedActions),
                GetHandler(performedActions, 1), GetHandler(performedActions, 2));
            
            // act
            var strMethodResult = proxy.StrMethod("t1");
            var taskIntMethodResult = await proxy.TaskIntMethod(5);
            await proxy.TaskVoidMethod();
            proxy.VoidMethod();
            var TaskIntMethodWithoutDelayResult = await proxy.TaskIntMethodWithoutDelay(6);
            
            // assert
            strMethodResult.Should().Be("t1 h2 h1");
            taskIntMethodResult.Should().Be(115);
            TaskIntMethodWithoutDelayResult.Should().Be(116);
            
            performedActions.Should().BeEquivalentTo(new List<string>
            {
                "StrMethod before 1 with args [\"t1\"]",
                "StrMethod before 2 with args [\"t1\"]",
                "StrMethod",
                "StrMethod after 2 with res t1",
                "StrMethod after 1 with res t1 h2",

                "TaskIntMethod before 1 with args [5]",
                "TaskIntMethod before 2 with args [5]",
                "TaskIntMethod",
                "TaskIntMethod after delay",
                "TaskIntMethod after 2 with res 5",
                "TaskIntMethod after 1 with res 105",

                "TaskVoidMethod before 1 with args []",
                "TaskVoidMethod before 2 with args []",
                "TaskVoidMethod",
                "TaskVoidMethod after delay",
                "TaskVoidMethod after 2 with res null",
                "TaskVoidMethod after 1 with res null",

                "VoidMethod before 1 with args []",
                "VoidMethod before 2 with args []",
                "VoidMethod",
                "VoidMethod after 2 with res null",
                "VoidMethod after 1 with res null",

                "TaskIntMethodWithoutDelay before 1 with args [6]",
                "TaskIntMethodWithoutDelay before 2 with args [6]",
                "TaskIntMethodWithoutDelay",
                "TaskIntMethodWithoutDelay after 2 with res 6",
                "TaskIntMethodWithoutDelay after 1 with res 106",
            }, o => o.WithStrictOrdering());
        }

        private static AopProxy.MethodCallHandler GetHandler(List<string> performedActions, int handlerNum)
        {
            return async (method, args, inner) =>
            {
                performedActions.Add($"{method.Name} before {handlerNum} with args {JsonConvert.SerializeObject(args)}");
                var o = await inner();
                performedActions.Add($"{method.Name} after {handlerNum} with res {o ?? "null"}");
                return o is int i
                    ? i + (int) Math.Pow(10, handlerNum)
                    : o is string s
                        ? s + " h" + handlerNum
                        : o;
            };
        }

        public interface ITest
        {
            string StrMethod(string s);
            Task<int> TaskIntMethod(int i);
            Task TaskVoidMethod();
            void VoidMethod();
            Task<int> TaskIntMethodWithoutDelay(int i);
        }

        private class Test : ITest
        {
            private readonly List<string> _performedActions;

            public Test(List<string> performedActions)
            {
                _performedActions = performedActions;
            }

            public string StrMethod(string s)
            {
                _performedActions.Add(nameof(StrMethod));
                return s;
            }

            public async Task<int> TaskIntMethod(int i)
            {
                _performedActions.Add(nameof(TaskIntMethod));
                await Task.Delay(1); 
                _performedActions.Add(nameof(TaskIntMethod) + " after delay");
                return i;
            }

            public async Task TaskVoidMethod()
            {
                _performedActions.Add(nameof(TaskVoidMethod));
                await Task.Delay(1); 
                _performedActions.Add(nameof(TaskVoidMethod) + " after delay");
            }

            public Task<int> TaskIntMethodWithoutDelay(int i)
            {
                _performedActions.Add(nameof(TaskIntMethodWithoutDelay));
                return Task.FromResult(i);
            }

            public void VoidMethod()
            {
                _performedActions.Add(nameof(VoidMethod));
            }
        }
    }
}