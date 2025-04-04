using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableGroupFactory : MonoBehaviour, ILaneDeployable<SubDivisionDataInBeat>
    {
        [SerializeField] GameObject noteDeployableColliderObj;

        List<NoteDeployableGroup> noteDeployableColliders = new List<NoteDeployableGroup>();

        void ILaneDeployable<SubDivisionDataInBeat>.Initialize()
        {
            foreach (NoteDeployableGroup noteDeployableCollider in noteDeployableColliders)
            {
                Destroy(noteDeployableCollider.gameObject);
            }

            noteDeployableColliders = new List<NoteDeployableGroup>();
        }

        GameObject ILaneDeployable<SubDivisionDataInBeat>.Deploy(SubDivisionDataInBeat subDivisionData, Vector3 pos, Transform parent)
        {
            GameObject obj = Instantiate(noteDeployableColliderObj);
            if (parent) { obj.transform.SetParent(parent); }
            obj.transform.localPosition = pos + new Vector3(0, 0.01f, 0);

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out NoteDeployableGroup line))
            {
                noteDeployableColliders?.Add(line);
            }

            return obj;
        }

        void ILaneDeployable<SubDivisionDataInBeat>.Scaling(float current, float previous)
        {
            foreach (NoteDeployableGroup noteDeployableCollider in noteDeployableColliders) 
            {
                Vector3 pos = noteDeployableCollider.gameObject.transform.position;
                noteDeployableCollider.gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z * (current / previous));
            }
        }
    }
}
