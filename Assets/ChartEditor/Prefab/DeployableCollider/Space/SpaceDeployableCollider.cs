using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SpaceDeployableCollider : MonoBehaviour, IFreedomDeployableCollider
    {
        [SerializeField] SpaceDeployableUnit spaceDeployableUnit;

        public EditMode EditMode => EditMode.SpaceDeploy;

        Transform IFreedomDeployableCollider.deployParent => spaceDeployableUnit.transform;

        AddressInChart IFreedomDeployableCollider.Address => spaceDeployableUnit.Address;
    }
}
