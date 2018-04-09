using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Infrastructure
{
    /// <summary>
    /// Helper to generate a calls wrapping proxy
    /// </summary>
    public class AopProxy : DispatchProxy
    {
        private static readonly ConcurrentDictionary<MethodInfo, MethodHelperFuncs> _cache =
            new ConcurrentDictionary<MethodInfo, MethodHelperFuncs>();

        private MethodCallHandler[] _handlers;
        private object _decorated;

        public delegate Task<object> MethodCallHandler(MethodInfo targetMethod, object[] args,
            Func<Task<object>> innerHandler);

        private void Initialize(object decorated, MethodCallHandler[] handlers)
        {
            _decorated = decorated;
            _handlers = handlers;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            // ReSharper disable once GenericEnumeratorNotDisposed
            var enumerator = _handlers.AsEnumerable().GetEnumerator();
            enumerator.MoveNext();
            var helperFuncs = _cache.GetOrAdd(targetMethod, BuildHelperFuncs);
            var result = enumerator.Current.Invoke(targetMethod, args,
                GetNextHandler(targetMethod, args, enumerator, helperFuncs));
            return helperFuncs.ConverterToActualReturnType(result);
        }

        private Func<Task<object>> GetNextHandler(MethodInfo targetMethod, object[] args,
            IEnumerator<MethodCallHandler> enumerator, MethodHelperFuncs methodHelperFuncs)
        {
            if (enumerator.MoveNext())
            {
                return () =>
                    enumerator.Current.Invoke(targetMethod, args,
                        GetNextHandler(targetMethod, args, enumerator, methodHelperFuncs));
            }
            else
            {
                // if no more wrappers found - call original method
                return () =>
                {
                    var originalMethodResult = methodHelperFuncs.InvokeFunc(_decorated, args);
                    return methodHelperFuncs.ConverterToObjectTask.Invoke(originalMethodResult);
                };
            }
        }

        /// <summary>
        /// Builds helper method funcs cache
        /// </summary>
        private static MethodHelperFuncs BuildHelperFuncs(MethodInfo targetMethod)
        {
            if (targetMethod.ReturnType.IsGenericType &&
                targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return (MethodHelperFuncs) typeof(AopProxy).GetMethod(nameof(BuildHelperFuncsForTask),
                        BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(targetMethod.ReturnType.GetGenericArguments()[0])
                    .Invoke(null, new object[] {targetMethod});
            }
            else if (targetMethod.ReturnType == typeof(Task))
            {
                return (MethodHelperFuncs) typeof(AopProxy).GetMethod(nameof(BuildHelperFuncsForVoidTask),
                        BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] {targetMethod});
            }
            else
            {
                return (MethodHelperFuncs) typeof(AopProxy).GetMethod(nameof(BuildHelperFuncsForNonTask),
                        BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] {targetMethod});
            }
        }

        [UsedImplicitly]
        private static MethodHelperFuncs BuildHelperFuncsForNonTask(MethodInfo methodInfo)
        {
            return new MethodHelperFuncs(Task.FromResult, t => t.Result, BuildInvokeFunc(methodInfo));
        }

        [UsedImplicitly]
        private static MethodHelperFuncs BuildHelperFuncsForVoidTask(MethodInfo methodInfo)
        {
            return new MethodHelperFuncs(async t =>
            {
                await (Task)t;
                return null;
            }, t => t, BuildInvokeFunc(methodInfo));
        }

        [UsedImplicitly]
        private static MethodHelperFuncs BuildHelperFuncsForTask<T>(MethodInfo methodInfo)
        {
            return new MethodHelperFuncs(async o => await (Task<T>) o, ConvertToTaskOfT<T>, BuildInvokeFunc(methodInfo));
        }

        private static Func<object, object[], object> BuildInvokeFunc(MethodInfo methodInfo)
        {
            try
            {

                var instanceParam = Expression.Parameter(typeof(object));
                var argsParam = Expression.Parameter(typeof(object[]));
                Expression expr = Expression.Call(Expression.Convert(instanceParam, methodInfo.DeclaringType),
                    methodInfo, GetMethodArgsExpressions(methodInfo, argsParam));
                if (methodInfo.ReturnType == typeof(void))
                    expr = Expression.Block(expr, Expression.Constant(null));
                
                return Expression.Lambda<Func<object, object[], object>>(expr, instanceParam, argsParam)
                    .Compile();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        private static IEnumerable<Expression> GetMethodArgsExpressions(MethodInfo targetMethod,
            ParameterExpression argsParam)
        {
            return targetMethod.GetParameters()
                .Select((p, i) =>
                    Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(i)), p.ParameterType));
        }
        
        private static async Task<T> ConvertToTaskOfT<T>(Task<object> t)
        {
            return (T) await t;
        }

        /// <summary>
        /// Creates a proxy, wrapping the method calls of <paramref name="decorated"/> into several other funcs,
        /// passed in <paramref name="handlers"/>.
        /// Each of them should call and await the underlying handler, passed as parameter.
        /// They are invoked in the order they are passed.
        /// Last of them instead of the next handler will receive the real method call as a parameter.
        /// </summary>
        public static T Create<T>([NotNull] T decorated, [NotNull] params MethodCallHandler[] handlers)
        {
            if (decorated == null) throw new ArgumentNullException(nameof(decorated));
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));
            if (handlers.Length == 0)
                throw new ArgumentException(nameof(handlers) + " array cannot be empty", nameof(handlers));

            object proxy = DispatchProxy.Create<T, AopProxy>();
            ((AopProxy) proxy).Initialize(decorated, handlers);
            return (T) proxy;
        }

        private class MethodHelperFuncs
        {
            public MethodHelperFuncs(
                Func<object, Task<object>> converterToObjectTask,
                Func<Task<object>, object> converterToActualReturnType, 
                Func<object, object[], object> invokeFunc)
            {
                ConverterToObjectTask = converterToObjectTask;
                ConverterToActualReturnType = converterToActualReturnType;
                InvokeFunc = invokeFunc;
            }

            public Func<object, Task<object>> ConverterToObjectTask { get; }
            public Func<Task<object>, object> ConverterToActualReturnType { get; }
            public Func<object, object[], object> InvokeFunc { get; }
        }
    }
}