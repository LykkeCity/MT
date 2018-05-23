using System.Collections.Generic;

namespace MarginTrading.Backend.Core.FakeExchangeConnector.Caches
{
    public interface IGenericDictionaryCache<T>
    {
        T Get(string key);

        IReadOnlyList<T> GetAll();

        void Set(T item);

        void SetAll(IEnumerable<T> items);

        void Clear(string key);

        void ClearAll();

        void Initialize(IEnumerable<T> items);
    }
}
