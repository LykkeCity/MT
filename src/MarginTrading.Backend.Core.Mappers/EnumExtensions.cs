using System;

namespace MarginTrading.Backend.Core.Mappers
{
    public static class EnumExtensions
    {
        public static TEnum ToType<TEnum>(this Enum dto)
            where TEnum : struct, IConvertible
        {
            if (!Enum.TryParse(dto.ToString(), out TEnum result))
            {
                throw new NotSupportedException($"Value {dto} is not suppoted by mapper");
            }

            return result;
        }
    }
}