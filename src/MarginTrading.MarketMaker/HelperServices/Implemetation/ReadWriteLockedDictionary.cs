using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.MarketMaker.HelperServices.Implemetation
{
    internal class ReadWriteLockedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private readonly IDictionary<TKey, TValue> _dictionary;
        private readonly Lock _lock = new Lock();

        public ReadWriteLockedDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public ReadWriteLockedDictionary(IEqualityComparer<TKey> equalityComparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(equalityComparer);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            KeyValuePair<TKey, TValue>[] array;

            using (_lock.EnterReadLock())
            {
                array = new KeyValuePair<TKey, TValue>[_dictionary.Count];
                _dictionary.CopyTo(array, 0);
            }

            return array.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            using (_lock.EnterWriteLock())
                _dictionary.Add(item);
        }

        public void Clear()
        {
            using (_lock.EnterWriteLock())
                _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            using (_lock.EnterReadLock())
                return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            using (_lock.EnterReadLock())
                _dictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            using (_lock.EnterWriteLock())
                return _dictionary.Remove(item);
        }

        public int Count
        {
            get
            {
                using (_lock.EnterReadLock())
                    return _dictionary.Count;
            }
        }

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            using (_lock.EnterWriteLock())
                _dictionary.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            using (_lock.EnterReadLock())
                return _dictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            using (_lock.EnterWriteLock())
                return _dictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            using (_lock.EnterReadLock())
                return _dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get
            {
                using (_lock.EnterReadLock())
                    return _dictionary[key];
            }
            set
            {
                using (_lock.EnterWriteLock())
                    _dictionary[key] = value;
            }
        }

        [CanBeNull]
        public TValue GetOrDefault(TKey key)
        {
            return GetOrDefault(key, k => default(TValue));
        }

        [CanBeNull]
        public TValue GetOrDefault(TKey key, Func<TKey, TValue> defaultValue)
        {
            using (_lock.EnterReadLock())
                return _dictionary.TryGetValue(key, out TValue value) ? value : defaultValue(key);
        }

        public ICollection<TKey> Keys
        {
            get
            {
                using (_lock.EnterReadLock())
                    return _dictionary.Keys.ToImmutableArray();
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                using (_lock.EnterReadLock())
                    return _dictionary.Values.ToImmutableArray();
            }
        }

        public void Dispose()
        {
            _lock?.Dispose();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            using (_lock.EnterUpgradeableReadLock())
            {
                if (!_dictionary.TryGetValue(key, out var value))
                {
                    value = valueFactory(key);
                    Add(key, value);
                }

                return value;
            }
        }

        public async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory)
        {
            using (_lock.EnterUpgradeableReadLock())
            {
                if (!_dictionary.TryGetValue(key, out var value))
                {
                    value = await valueFactory(key);
                    Add(key, value);
                }

                return value;
            }
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> valueFactory,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            using (_lock.EnterUpgradeableReadLock())
            {
                var value = !_dictionary.TryGetValue(key, out var oldValue)
                    ? valueFactory(key)
                    : updateValueFactory(key, oldValue);
                this[key] = value;
                return value;
            }
        }

        public async Task<TValue> AddOrUpdateAsync(TKey key, Func<TKey, Task<TValue>> valueFactory,
            Func<TKey, TValue, Task<TValue>> updateValueFactory)
        {
            using (_lock.EnterUpgradeableReadLock())
            {
                var value = await (!_dictionary.TryGetValue(key, out var oldValue)
                    ? valueFactory(key)
                    : updateValueFactory(key, oldValue));
                this[key] = value;
                return value;
            }
        }

        private class Lock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

            public void Dispose()
            {
                _lock?.Dispose();
            }

            public IDisposable EnterReadLock()
            {
                _lock.EnterReadLock();
                return new Disposable(_lock, l => l.ExitReadLock());
            }

            public IDisposable EnterWriteLock()
            {
                _lock.EnterWriteLock();
                return new Disposable(_lock, l => l.ExitWriteLock());
            }

            public IDisposable EnterUpgradeableReadLock()
            {
                _lock.EnterUpgradeableReadLock();
                return new Disposable(_lock, l => l.ExitUpgradeableReadLock());
            }

            private struct Disposable : IDisposable
            {
                private readonly Action<ReaderWriterLockSlim> _onDispose;
                private readonly ReaderWriterLockSlim _locker;

                public Disposable(ReaderWriterLockSlim locker, Action<ReaderWriterLockSlim> onDispose)
                {
                    _onDispose = onDispose;
                    _locker = locker;
                }

                public void Dispose()
                {
                    _onDispose(_locker);
                }
            }
        }
    }
}