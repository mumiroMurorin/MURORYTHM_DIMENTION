using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SpaceDeployableGroupFactory : MonoBehaviour, ILaneDeployable<SubDivisionDataInBeat>, ILayerAffectable, IScaleAffectable
    {
        [SerializeField] GameObject spaceDeployableColliderObj;
        [SerializeField] float heightOnGroundEditMode;
        [SerializeField] float heightOnSpaceEditMode;

        List<SpaceDeployableUnit> spaceDeployableColliders = new List<SpaceDeployableUnit>();

        void ILaneDeployable<SubDivisionDataInBeat>.Initialize()
        {
            foreach (var spaceDeployable in spaceDeployableColliders)
            {
                Destroy(spaceDeployable.gameObject);
            }

            spaceDeployableColliders = new List<SpaceDeployableUnit>();
        }

        GameObject ILaneDeployable<SubDivisionDataInBeat>.Deploy(SubDivisionDataInBeat subDivisionData, Vector3 pos, Transform parent)
        {
            GameObject obj = Instantiate(spaceDeployableColliderObj);
            if (parent) { obj.transform.SetParent(parent); }
            obj.transform.localPosition = pos;

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out SpaceDeployableUnit line))
            {
                line.SetAddress(subDivisionData.BarIndex, subDivisionData.SubDivisionIndex);
                subDivisionData.SetSpaceLocation(line.GetNoteDeployableUnitTransforms());
                spaceDeployableColliders?.Add(line);
            }

            return obj;
        }

        void ILayerAffectable.OnChangeLayer(EditNoteType editNoteType)
        {
            foreach(var deployable in spaceDeployableColliders)
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

        void IScaleAffectable.OnChangeSize(float z)
        {
            foreach (var col in spaceDeployableColliders)
            {
                col.ChangeSize(z);
            }
        }
    }
}
