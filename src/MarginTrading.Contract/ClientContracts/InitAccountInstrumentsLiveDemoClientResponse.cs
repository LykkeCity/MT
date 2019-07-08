// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class InitAccountInstrumentsLiveDemoClientResponse
    {
        public InitAccountInstrumentsClientResponse Live { get; set; }
        public InitAccountInstrumentsClientResponse Demo { get; set; }
    }
}