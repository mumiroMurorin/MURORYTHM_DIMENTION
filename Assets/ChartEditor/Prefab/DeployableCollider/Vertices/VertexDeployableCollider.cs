using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class VertexDeployableCollider : MonoBehaviour, IPointDeployableCollider
    {
        public EditMode EditMode => EditMode.None;
    }

}
