using UnityEngine;
using UniRx;
using VContainer;
using Deform;

public class GroundOptionSetter : MonoBehaviour
{
    [SerializeField] GameObject[] divisionLines;
    [SerializeField] BendDeformer bendDeformer;
    [SerializeField] Transform groundObjectRoot;
    [SerializeField, Min(0f)] float visibleDistancePadding;

    IOptionGetter optionGetter;
    // カーブと奥行きの表示範囲 (譜面エディタの全画面プレビューと同じ処理)
    GroundShaper shaper;

    [Inject]
    public void Construct(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    private void Start()
    {
        shaper = new GroundShaper(bendDeformer, groundObjectRoot, visibleDistancePadding);
        shaper.InitializeVisibility();
        Bind();
    }

    private void Bind()
    {
        optionGetter?.GroundDivisionNum
            .Subscribe(divNum => GroundShaper.SetDivisionLines(divisionLines, divNum))
            .AddTo(this.gameObject);

        optionGetter?.NoteCurveRadius
            .Subscribe(shaper.SetCurveRadius)
            .AddTo(this.gameObject);

        optionGetter?.NoteVisibleDistance
            .Subscribe(shaper.SetVisibleDistance)
            .AddTo(this.gameObject);
    }

    private void OnDestroy()
    {
        shaper?.Dispose();
    }
}
