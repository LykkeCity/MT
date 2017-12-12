namespace MarginTrading.Backend.Services.Infrastructure
{
    public interface ICachedCalculation<out TResult>
    {
        TResult Get();
    }
}