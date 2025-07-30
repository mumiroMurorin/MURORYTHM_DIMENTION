using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SubdivisionLineFactory : MonoBehaviour, ILaneDeployable
    {
        [SerializeField] GameObject subdivisionLinePrefab;

        DeployableLineObject ILaneDeployable.Deploy(Transform parent)
        {
            GameObject obj = Instantiate(subdivisionLinePrefab);
            if (parent) { obj.transform.SetParent(parent); }
            if (!obj.TryGetComponent(out DeployableLineObject deployable)) 
            {
                Debug.Log("【Lane】DeployableLineObjectがアタッチされていません");
            }

            return deployable;
        }
    }
}
