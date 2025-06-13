using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableGroupFactory : MonoBehaviour, ILaneDeployable<SubDivisionDataInBeat>, ILayerAffectable
    {
        [SerializeField] GameObject groundDeployableColliderObj;
        [SerializeField] float heightOnGroundEditMode;
        [SerializeField] float heightOnSpaceEditMode;

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
            GameObject obj = Instantiate(groundDeployableColliderObj);
            if (parent) { obj.transform.SetParent(parent); }
            obj.transform.localPosition = pos + new Vector3(0, 0.01f, 0);

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out NoteDeployableGroup line))
            {
                line.SetAddress(subDivisionData.BarIndex, subDivisionData.SubDivisionIndex);
                subDivisionData.SetPlacementLocation(line.GetNoteDeployableUnitTransforms());
                noteDeployableColliders?.Add(line);
            }

            return obj;
        }

        void ILayerAffectable.OnChangeLayer(EditNoteType editNoteType)
        {
            foreach (var deployable in noteDeployableColliders)
            {
                Vector3 pos = deployable.gameObject.transform.position;
                switch (editNoteType)
                {
                    case EditNoteType.Ground:
                        deployable.gameObject.transform.position = new Vector3(pos.x, heightOnGroundEditMode, pos.z);
                        break;
                    case EditNoteType.Space:
                        deployable.gameObject.transform.position = new Vector3(pos.x, heightOnSpaceEditMode, pos.z);
                        break;
                }
            }
        }
    }
}
