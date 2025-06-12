using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableCollider : MonoBehaviour, IDeployableCollider
    {
        [SerializeField] NoteDeployableUnit noteDeployableUnit;

        public EditMode EditMode => EditMode.Deploy;

        Transform IDeployableCollider.deployParent => noteDeployableUnit.transform;

        AddressInChart IDeployableCollider.Address => noteDeployableUnit.Address;
    }
}
