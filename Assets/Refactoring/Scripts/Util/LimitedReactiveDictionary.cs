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
        // �v�f�����ő�l�𒴂���ꍇ�A�ŏ��̗v�f���폜
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

        // �V�����v�f��ǉ�
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
