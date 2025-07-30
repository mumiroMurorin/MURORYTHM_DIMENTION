using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class BarLineFactory : MonoBehaviour, ILaneDeployable
    {
        [SerializeField] GameObject barLinePrefab;

        DeployableLineObject ILaneDeployable.Deploy(Transform parent)
        {
            GameObject obj = Instantiate(barLinePrefab);
            if (parent) { obj.transform.SetParent(parent); }
            if (!obj.TryGetComponent(out DeployableLineObject deployable))
            {
                Debug.Log("【Lane】DeployableLineObjectがアタッチされていません");
            }

            return deployable;
        }
    }
}
