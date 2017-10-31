namespace MarginTrading.Contract.ClientContracts
{
    public class MtClientResponse<T>
    {
        public T Result { get; set; }
        public string Message { get; set; }

        public bool IsError()
        {
            return !string.IsNullOrEmpty(Message);
        }
    }
}
