using FluentAssertions.Primitives;

namespace MarginTradingTests.Helpers
{
    public static class FluentAssertionExtensions
    {
        public static TypedAssertions<T> Should<T>(this T actualValue)
        {
            return new TypedAssertions<T>(actualValue);
        }

        public class TypedAssertions<T> : ReferenceTypeAssertions<T, TypedAssertions<T>>
        {
            protected override string Context => "object of type " + nameof(T);
            public TypedAssertions(T subject) => this.Subject = subject;
        }
    }
}
