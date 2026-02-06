using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace CustomSerialization
{
    [Serializable, Preserve]
    public struct SerializableKeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
        
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
        
        public override string ToString()
        {
            return $"Key: {Key}, Value: {Value}";
        }
    }
    
    [Serializable, Preserve]
    public class SerializableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        IReadOnlyDictionary<TKey, TValue>,
        ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializableKeyValuePair<TKey, TValue>> _keyValuePairs = new();
        private Dictionary<TKey, TValue> _dictionary = new();
        
        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        
        public TValue this[TKey key]
        {
            get
            {
                if (_dictionary.TryGetValue(key, out var item) == false) throw new KeyNotFoundException();
                return item;
            }
            set
            {
                //if (_dictionary.ContainsKey(key) == false) throw new KeyNotFoundException();
                
                _dictionary[key] = value;
                var index = _keyValuePairs.FindIndex(kvp => kvp.Key.Equals(key));
                if (index >= 0) _keyValuePairs[index] = new SerializableKeyValuePair<TKey, TValue>(key, value);
                else _keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
            }
        }
        
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;
        
        #region IDictionary 인터페이스 함수 구현
        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            _keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
        }
        
        public bool Remove(TKey key)
        {
            if (_dictionary.Remove(key) == false) return false;
            
            var index = _keyValuePairs.FindIndex(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
            if (index >= 0) _keyValuePairs.RemoveAt(index);
            return true;
        }
        
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }
        
        public void Clear()
        {
            _dictionary.Clear();
            _keyValuePairs.Clear();
        }
        
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }
        
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }
        
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        }
        
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        #endregion
        
        #region ISerializationCallbackReceiver 인터페이스 함수 구현
        public void OnBeforeSerialize()
        {
            _keyValuePairs.Clear();
            foreach (var kvp in _dictionary)
            {
                _keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
            }
        }
        public void OnAfterDeserialize()
        {
            _dictionary = _keyValuePairs.ToDictionary(x => x.Key, x => x.Value);
        }
        #endregion
        
        public bool TryAdd(TKey key, TValue value) => _dictionary.TryAdd(key, value);
        
        public override string ToString()
        {
            return $"{{ {string.Join(", ", _dictionary.Select(kvp => $"{kvp.Key}: {kvp.Value}"))} }}";
        }
    }
}
