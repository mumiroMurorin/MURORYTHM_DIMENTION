using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class StageLoader : MonoBehaviour
{
    [SerializeField] Transform stageParent;
    [SerializeField] GameObject stagePrefab;

    IMusicDataGetter musicDataGetter;
    
    [Inject]
    public void Constructor(IMusicDataGetter musicDataGetter)
    {
        this.musicDataGetter = musicDataGetter;
    }

    private void Start()
    {
        if(stagePrefab == null) { return; }
        if(stagePrefab == null) { return; }

        var obj = Instantiate(stagePrefab, Vector3.zero, Quaternion.identity, stageParent);

        if(!obj.TryGetComponent(out IStageController stageController))
        {
            Debug.LogWarning($"【System】ステージオブジェクトにIStageControllerがアタッチされていません: {stagePrefab.name}");
            return;
        }

        stageController.Initialize(musicDataGetter);
    }
}