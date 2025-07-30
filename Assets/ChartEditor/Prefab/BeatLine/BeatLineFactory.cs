using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class BeatLineFactory : MonoBehaviour, ILaneDeployable
    {
        [SerializeField] GameObject beatLinePrefab;

        DeployableLineObject ILaneDeployable.Deploy(Transform parent)
        {
            GameObject obj = Instantiate(beatLinePrefab);
            if (parent) { obj.transform.SetParent(parent); }
            if (!obj.TryGetComponent(out DeployableLineObject deployable))
            {
                Debug.Log("【Lane】DeployableLineObjectがアタッチされていません");
            }

            return deployable;
        }
    }
}
