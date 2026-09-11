using UnityEngine;

public static class NoteLayerUtility
{
    public const int NotesLayer = 11;
    public const int DefaultNoteSortingOrder = 30;

    public static void SetNotesLayer(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        target.layer = NotesLayer;
    }

    public static void SetNotesLayerRecursively(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        SetNotesLayerRecursively(target.transform);
    }

    public static void SetSortingOrderRecursively(GameObject target, int sortingOrder)
    {
        if (target == null)
        {
            return;
        }

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static void SetNotesLayerRecursively(Transform target)
    {
        target.gameObject.layer = NotesLayer;

        foreach (Transform child in target)
        {
            SetNotesLayerRecursively(child);
        }
    }
}
