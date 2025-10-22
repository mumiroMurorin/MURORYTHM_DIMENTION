using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class GroundContoroller : MonoBehaviour
{
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    INoteSpawnDataOptionHolder optionHolder;

    [Inject]
    public void Constructor(INoteSpawnDataOptionHolder optionHolder)
    {
        this.optionHolder = optionHolder;
    }

    private void Update()
    {
        MoveGround();
    }

    /// <summary>
    /// グラウンドを動かす
    /// </summary>
    private void MoveGround()
    {
        // 譜面を進める
        if (timer == null || timer.Value == null) { return; }
        this.gameObject.transform.position = Vector3.back * optionHolder.NoteSpeed.Value * timer.Value.Time;
    }
}
