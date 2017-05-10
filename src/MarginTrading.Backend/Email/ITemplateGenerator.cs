#pragma warning disable 1591

namespace MarginTrading.Backend.Email
{
    public interface ITemplateGenerator
    {
        string Generate<T>(string templateName, T model);
    }
}
