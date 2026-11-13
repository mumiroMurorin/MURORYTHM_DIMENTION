using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// スペースノーツの配置 (分線ごとに1か所の配置場所へ置く)
    /// </summary>
    public class SpaceDeployer : NoteDeployerBase
    {
        protected override EditMode DeployMode => EditMode.SpaceDeploy;

        protected override int DeployRangeCount => 1;

        protected override IInteractableCollider GetDeployColliderUnderCursor()
        {
            return dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();
        }

        protected override bool TryGetDeployLocation(IInteractableCollider collider, out Transform parent, out AddressInChart address)
        {
            if (collider is IFreedomDeployableCollider deployable)
            {
                parent = deployable.deployParent;
                address = deployable.Address;
                return true;
            }

            parent = null;
            address = null;
            return false;
        }
    }
}
