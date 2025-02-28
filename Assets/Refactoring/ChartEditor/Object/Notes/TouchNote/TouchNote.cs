using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class TouchNote : MonoBehaviour , IDeployableObject, IMovableObject
    {
        [SerializeField] Renderer _renderer;
        [SerializeField] Collider[] _colliders;

        void IDeployableObject.OnDeploy()
        {
            _renderer.material.color *= new Color(1, 1, 1, 2f);

            EnableCollider(true);
        }

        void IDeployableObject.OnInstantiate()
        {
            _renderer.material.color *= new Color(1, 1, 1, 0.5f);

            EnableCollider(false);
        }

        private void EnableCollider(bool isActive)
        {
            foreach (Collider collider in _colliders)
            {
                collider.enabled = isActive;
            }
        }

        void IMovableObject.OnMoveStart()
        {
            transform.position += new Vector3(0, 2f, 0);
        }

        void IMovableObject.OnMoveEnd()
        {
            transform.position -= new Vector3(0, 2f, 0);
        }

        void IMovableObject.OnMove()
        {

        }
    }

}