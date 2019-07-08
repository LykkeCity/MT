// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
