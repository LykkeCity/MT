using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    public static class ShallowCopyHelper<T>
    {
        private static Action<T, T> copier = CreateCopier();

        private static Action<T, T> CreateCopier()
        {
            var method = new DynamicMethod("CloneImplementation", typeof(void), new[] { typeof(T), typeof(T) }, true);
            var generator = method.GetILGenerator();
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Stfld, field);
            }

            generator.Emit(OpCodes.Ret);
            return (Action<T, T>)method.CreateDelegate(typeof(Action<T, T>));
        }

        public static void Copy(T src, T target)
        {
            copier(src, target);
        }
    }
}
