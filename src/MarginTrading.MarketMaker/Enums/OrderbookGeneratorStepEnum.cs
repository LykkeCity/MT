namespace MarginTrading.MarketMaker.Enums
{
    public enum OrderbookGeneratorStepEnum
    {
        FindOutdated = 30,
        FindOutliers = 40,
        FindRepeatedProblems = 50,
        ChoosePrimary = 60,
        Transform = 80,
    }
}
