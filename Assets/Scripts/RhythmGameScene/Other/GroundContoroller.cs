using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class GroundContoroller : MonoBehaviour
{
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    INoteSpawnDataOptionHolder optionHolder;

    [Inject]
    public void Constructor(INoteSpawnDataOptionHolder optionHolder)
    {
        this.optionHolder = optionHolder;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        if(timer == null) { return; }
        if(timer.Value == null) { return; }

        timer.Value.TimeRP
            .Subscribe(MoveGround)
            .AddTo(this.gameObject);
    }

    /// <summary>
    /// ƒOƒ‰ƒEƒ“ƒh‚ð“®‚©‚·
    /// </summary>
    private void MoveGround(float time)
    {
        this.gameObject.transform.position = Vector3.back * optionHolder.NoteSpeed.Value * time;
    }
}
