using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VContainer;

public class StageLoader : MonoBehaviour
{
    [SerializeField] Transform stageParent;
    [SerializeField] StageTypeToObject[] stageTypeToPrefabs;

    IMusicDataGetter musicDataGetter;
    
    [Inject]
    public void Constructor(IMusicDataGetter musicDataGetter)
    {
        this.musicDataGetter = musicDataGetter;
    }

    private void Start()
    {
        var obj = InstantiateStagePrefab(musicDataGetter.Music.Value.StageType);
        if(obj == null) { return; }

        obj.transform.SetParent(stageParent);

        if(!obj.TryGetComponent(out IStageController stageController))
        {
            Debug.LogWarning($"【System】ステージオブジェクトにIStageControllerがアタッチされていません: {obj.name}");
            return;
        }

        stageController.Initialize(musicDataGetter);
    }

    private GameObject InstantiateStagePrefab(StageType stageType)
    {
        if (stageTypeToPrefabs == null) 
        {
            Debug.LogError($"【System】ステージ配列の長さが0です");
            return null; 
        }

        foreach (var pair in stageTypeToPrefabs)
        {
            GameObject obj = pair.CheckAndGetPrefab(stageType);

            if (obj == null) { continue; }

            // 該当したらインスタンス化して返す
            return Instantiate(obj, Vector3.zero, Quaternion.identity);
        }

        Debug.LogError($"【System】該当するStageが見つかりません: {stageType}");
        return null;
    }

    [Serializable]
    private class StageTypeToObject
    {
        [SerializeField] StageType stageType;
        [SerializeField] GameObject prefab;

        public GameObject CheckAndGetPrefab(StageType type)
        {
            if(stageType != type) { return null; }
            return prefab;
        }
    }
}