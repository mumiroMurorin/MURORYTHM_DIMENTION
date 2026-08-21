using UnityEngine;

public static class NoteLayerUtility
{
    public const int NotesLayer = 11;

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

    private static void SetNotesLayerRecursively(Transform target)
    {
        target.gameObject.layer = NotesLayer;

        foreach (Transform child in target)
        {
            SetNotesLayerRecursively(child);
        }
    }
}
