using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableGroupFactory : MonoBehaviour, ILaneDeployable
    {
        [SerializeField] Transform noteDeployableColliderParent;
        [SerializeField] GameObject noteDeployableColliderObj;

        List<NoteDeployableGroup> noteDeployableColliders = new List<NoteDeployableGroup>();

        void ILaneDeployable.Initialize()
        {
            foreach (NoteDeployableGroup noteDeployableCollider in noteDeployableColliders)
            {
                Destroy(noteDeployableCollider.gameObject);
            }

            noteDeployableColliders = new List<NoteDeployableGroup>();
        }

        GameObject ILaneDeployable.Deploy(Vector3 pos)
        {
            GameObject obj = Instantiate(noteDeployableColliderObj);
            if (noteDeployableColliderParent) { obj.transform.SetParent(noteDeployableColliderParent); }
            obj.transform.localPosition = pos + new Vector3(0, 0.01f, 0);

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out NoteDeployableGroup line))
            {
                noteDeployableColliders?.Add(line);
            }

            return obj;
        }

        float currentScale = 1f;
        void ILaneDeployable.Scaling(float scale)
        {
            foreach (NoteDeployableGroup noteDeployableCollider in noteDeployableColliders) 
            {
                Vector3 pos = noteDeployableCollider.gameObject.transform.position;
                noteDeployableCollider.gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z * (scale / currentScale));
            }

            currentScale = scale;
        }
    }
}
