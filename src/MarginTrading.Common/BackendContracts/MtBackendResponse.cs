namespace MarginTrading.Common.BackendContracts
{
    public class MtBackendResponse<T>
    {
        public T Result { get; set; }
        public string Message { get; set; }
    }
}
