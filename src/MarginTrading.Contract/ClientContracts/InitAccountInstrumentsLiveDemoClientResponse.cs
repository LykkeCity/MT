// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class InitAccountInstrumentsLiveDemoClientResponse
    {
        public InitAccountInstrumentsClientResponse Live { get; set; }
        public InitAccountInstrumentsClientResponse Demo { get; set; }
    }
}