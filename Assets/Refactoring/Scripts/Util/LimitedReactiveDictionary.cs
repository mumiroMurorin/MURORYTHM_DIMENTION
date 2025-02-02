using UniRx;
using UnityEngine;

public class LimitedReactiveDictionary<TKey, TValue>
{
    private readonly int _maxCount;
    private readonly ReactiveDictionary<TKey, TValue> _dictionary = new ReactiveDictionary<TKey, TValue>();

    public IReadOnlyReactiveDictionary<TKey, TValue> Dictionary => _dictionary;

    public LimitedReactiveDictionary(int maxCount)
    {
        _maxCount = maxCount;
    }

    public void Add(TKey key, TValue value)
    {
        // 要素数が最大値を超える場合、最初の要素を削除
        if (_dictionary.Count >= _maxCount)
        {
            var firstKey = default(TKey);
            foreach (var item in _dictionary)
            {
                firstKey = item.Key;
                break;
            }

            if (firstKey != null)
            {
                _dictionary.Remove(firstKey);
            }
        }

        // 新しい要素を追加
        _dictionary.Add(key, value);
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
