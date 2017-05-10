#pragma warning disable 1591

namespace MarginTrading.Common.Models
{
    public class MtResponse<T> : MtResponse
    {
        public T Result { get; set; }
    }

    public class MtResponse
    {
        public string Message { get; set; }
    }
}
