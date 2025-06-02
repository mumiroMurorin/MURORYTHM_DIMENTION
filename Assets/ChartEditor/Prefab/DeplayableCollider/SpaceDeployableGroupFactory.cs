using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SpaceDeployableGroupFactory : MonoBehaviour, ILaneDeployable<SubDivisionDataInBeat>
    {
        [SerializeField] GameObject spaceDeployableColliderObj;
 
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
            obj.transform.localPosition = pos + new Vector3(0, -5f, 0);

            // ê∂ê¨ÇµÇΩÉâÉCÉìÇÉäÉXÉgÇ…äiî[
            if (obj.TryGetComponent(out SpaceDeployableUnit line))
            {
                line.SetAddress(subDivisionData.BarIndex, subDivisionData.SubDivisionIndex);
                //subDivisionData.SetPlacementLocation(line.GetNoteDeployableUnitTransforms());
                spaceDeployableColliders?.Add(line);
            }

            return obj;
        }
    }
}
