using UniRx;
using System.Collections.Generic;
using System.Linq;

public static class ReactiveCollectionExtensions
{
    public static void Rotate<T>(this ReactiveCollection<T> collection, int delta)
    {
        int count = collection.Count;
        if (count == 0) { return; }

        delta = ((delta % count) + count) % count; // ³‹K‰»

        if (delta == 0) { return; }

        // ”²‚«o‚·––”ö•”•ª‚ğ•Û‘¶
        List<T> tail = new List<T>();
        for (int i = count - delta; i < count; i++)
        {
            tail.Add(collection[i]);
        }

        // ––”ö•”•ª‚ğíœiŒã‚ë‚©‚ç‡‚Éj
        for (int i = 0; i < delta; i++)
        {
            collection.RemoveAt(collection.Count - 1);
        }

        // æ“ª‚É‘}“üi‘O‚©‚ç‡‚Éj
        for (int i = delta - 1; i >= 0; i--)
        {
            collection.Insert(0, tail[i]);
        }
    }

    /// <summary>
    /// ReactiveCollection‚Ì—v‘f‚ğ”½“]‚³‚¹‚é
    /// </summary>
    public static void ReverseElements<T>(this ReactiveCollection<T> collection)
    {
        if (collection == null || collection.Count <= 1) return;

        // Œ»İ‚Ì—v‘f‚ğ‹t‡‚É•Û‘¶
        var reversed = collection.Reverse().ToList();

        // Œ³‚Ì—v‘f‚ğ‚·‚×‚Äíœ
        while (collection.Count > 0)
        {
            collection.RemoveAt(collection.Count - 1);
        }

        // ‹t‡‚É’Ç‰Á
        foreach (var item in reversed)
        {
            collection.Add(item);
        }
    }
}