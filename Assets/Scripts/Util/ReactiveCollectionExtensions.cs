using UniRx;
using System.Collections.Generic;
using System.Linq;

public static class ReactiveCollectionExtensions
{
    public static void Rotate<T>(this ReactiveCollection<T> collection, int delta)
    {
        int count = collection.Count;
        if (count == 0) { return; }

        delta = ((delta % count) + count) % count; // 正規化

        if (delta == 0) { return; }

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

    /// <summary>
    /// ReactiveCollectionの要素を反転させる
    /// </summary>
    public static void ReverseElements<T>(this ReactiveCollection<T> collection)
    {
        if (collection == null || collection.Count <= 1) return;

        // 現在の要素を逆順に保存
        var reversed = collection.Reverse().ToList();

        // 元の要素をすべて削除
        while (collection.Count > 0)
        {
            collection.RemoveAt(collection.Count - 1);
        }

        // 逆順に追加
        foreach (var item in reversed)
        {
            collection.Add(item);
        }
    }
}