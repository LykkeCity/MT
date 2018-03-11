using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace MarginTrading.Common.Helpers
{
    public class ReadWriteLockedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDisposable
    {
        private readonly IDictionary<TKey, TValue> _dictionary;
        private readonly ReaderWriterLockHelper _lock = new ReaderWriterLockHelper();

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

        /// <summary>
        /// Access thread-unsafe value inside read lock, calculate arbitrary value (should be fast-performing)
        /// and return result
        /// </summary>
        public TResult TryReadValue<TResult>(TKey key, Func<bool, TKey, TValue, TResult> readFunc)
        {
            using (_lock.EnterReadLock())
            {
                var success = _dictionary.TryGetValue(key, out var value);
                return readFunc(success, key, value);
            }
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

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        [CanBeNull]
        public TValue GetOrDefault(TKey key)
        {
            return GetOrDefault(key, k => default(TValue));
        }

        [CanBeNull]
        public TValue GetOrDefault(TKey key, Func<TKey, TValue> defaultValue)
        {
            using (_lock.EnterReadLock())
                return _dictionary.TryGetValue(key, out var value) ? value : defaultValue(key);
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
        
        public ImmutableArray<TKey> Keys
        {
            get
            {
                using (_lock.EnterReadLock())
                    return _dictionary.Keys.ToImmutableArray();
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;
        
        public ImmutableArray<TValue> Values
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

        public bool UpdateIfExists(TKey key, Func<TKey, TValue, TValue> updateValueFactory)
        {
            using (_lock.EnterUpgradeableReadLock())
            {
                if (_dictionary.TryGetValue(key, out var oldValue))
                {
                    var value = updateValueFactory(key, oldValue);
                    this[key] = value;
                    return true;
                }
            }

            return false;
        }
    }
}