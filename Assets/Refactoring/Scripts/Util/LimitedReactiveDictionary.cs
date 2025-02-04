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
        // ����Dictionary�Ɋ܂܂�Ă���ꍇ�l��ύX����
        if (_dictionary.ContainsKey(key))
        {
            _dictionary[key] = value;
            _keyOrder.Remove(key);
            _keyOrder.AddLast(key);
            return;
        }

        // �v�f�����ő�l�𒴂���ꍇ�A�ł��Â��v�f���폜
        if (_dictionary.Count + 1 > _maxCount)
        {
            var oldestKey = _keyOrder.First.Value;
            _dictionary.Remove(oldestKey);
            _keyOrder.RemoveFirst();
        }

        // �V�����v�f��ǉ�
        _dictionary.Add(key, value);
        // Add���Ǘ��̂��߂�LinkedList�ɂ��ǉ�
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
