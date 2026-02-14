using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("_keyValuePairs")]
        private List<SerializableKeyValuePair<TKey, TValue>> m_keyValuePairs = new();
        private Dictionary<TKey, TValue> m_dictionary = new();
        
        public ICollection<TKey> Keys => m_dictionary.Keys;
        public ICollection<TValue> Values => m_dictionary.Values;
        
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        
        public TValue this[TKey key]
        {
            get
            {
                if (m_dictionary.TryGetValue(key, out var item) == false) throw new KeyNotFoundException();
                return item;
            }
            set
            {
                //if (m_dictionary.ContainsKey(key) == false) throw new KeyNotFoundException();
                
                m_dictionary[key] = value;
                var index = m_keyValuePairs.FindIndex(kvp => kvp.Key.Equals(key));
                if (index >= 0) m_keyValuePairs[index] = new SerializableKeyValuePair<TKey, TValue>(key, value);
                else m_keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
            }
        }
        
        public int Count => m_dictionary.Count;
        public bool IsReadOnly => false;
        
        #region IDictionary 인터페이스 함수 구현
        public void Add(TKey key, TValue value)
        {
            m_dictionary.Add(key, value);
            m_keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
        }
        
        public bool Remove(TKey key)
        {
            if (m_dictionary.Remove(key) == false) return false;
            
            var index = m_keyValuePairs.FindIndex(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
            if (index >= 0) m_keyValuePairs.RemoveAt(index);
            return true;
        }
        
        public bool ContainsKey(TKey key)
        {
            return m_dictionary.ContainsKey(key);
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            return m_dictionary.TryGetValue(key, out value);
        }
        
        public void Clear()
        {
            m_dictionary.Clear();
            m_keyValuePairs.Clear();
        }
        
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }
        
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return m_dictionary.Contains(item);
        }
        
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)m_dictionary).CopyTo(array, arrayIndex);
        }
        
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return m_dictionary.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        #endregion
        
        #region ISerializationCallbackReceiver 인터페이스 함수 구현
        public void OnBeforeSerialize()
        {
            m_keyValuePairs.Clear();
            foreach (var kvp in m_dictionary)
            {
                m_keyValuePairs.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
            }
        }
        public void OnAfterDeserialize()
        {
            m_dictionary = m_keyValuePairs.ToDictionary(x => x.Key, x => x.Value);
        }
        #endregion
        
        public bool TryAdd(TKey key, TValue value) => m_dictionary.TryAdd(key, value);
        
        public override string ToString()
        {
            return $"{{ {string.Join(", ", m_dictionary.Select(kvp => $"{kvp.Key}: {kvp.Value}"))} }}";
        }
    }
}
