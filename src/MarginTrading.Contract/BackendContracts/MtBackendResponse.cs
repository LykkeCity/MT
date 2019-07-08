// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts
{
    public class MtBackendResponse<T>
    {
        public T Result { get; set; }
        public string ErrorMessage { get; set; }

        public static MtBackendResponse<T> Ok(T result)
        {
            return new MtBackendResponse<T>
            {
                Result = result
            };
        }
        
        public static MtBackendResponse<T> Error(string message)
        {
            return new MtBackendResponse<T>
            {
                ErrorMessage = message
            };
        }
    }
}
