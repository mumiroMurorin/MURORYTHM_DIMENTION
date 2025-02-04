using UniRx;
using UnityEngine;
using System.Collections.Generic;

public class LimitedReactiveDictionary<TKey, TValue>
{
    private readonly int _maxCount;
    private readonly ReactiveDictionary<TKey, TValue> _dictionary = new ReactiveDictionary<TKey, TValue>();
    private LinkedList<TKey> _keyOrder = new LinkedList<TKey>();

    public IReadOnlyReactiveDictionary<TKey, TValue> Dictionary => _dictionary;

    public LimitedReactiveDictionary(int maxCount)
    {
        _maxCount = maxCount;
    }

    public void Add(TKey key, TValue value)
    {
        // 既にDictionaryに含まれている場合値を変更する
        if (_dictionary.ContainsKey(key))
        {
            _dictionary[key] = value;
            _keyOrder.Remove(key);
            _keyOrder.AddLast(key);
            return;
        }

        // 要素数が最大値を超える場合、最も古い要素を削除
        if (_dictionary.Count + 1 > _maxCount)
        {
            var oldestKey = _keyOrder.First.Value;
            _dictionary.Remove(oldestKey);
            _keyOrder.RemoveFirst();
        }

        // 新しい要素を追加
        _dictionary.Add(key, value);
        // Add順管理のためにLinkedListにも追加
        _keyOrder.AddLast(key);
    }

    public void Remove(TKey key)
    {
        _dictionary.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }
}
