using UniRx;
using System.Collections.Generic;

public static class ReactiveCollectionExtensions
{
    public static void Rotate<T>(this ReactiveCollection<T> collection, int delta)
    {
        int count = collection.Count;
        if (count == 0) return;

        delta = ((delta % count) + count) % count; // 正規化

        if (delta == 0) return;

        // 抜き出す末尾部分を保存
        List<T> tail = new List<T>();
        for (int i = count - delta; i < count; i++)
        {
            tail.Add(collection[i]);
        }

        // 末尾部分を削除（後ろから順に）
        for (int i = 0; i < delta; i++)
        {
            collection.RemoveAt(collection.Count - 1);
        }

        // 先頭に挿入（前から順に）
        for (int i = delta - 1; i >= 0; i--)
        {
            collection.Insert(0, tail[i]);
        }
    }
}