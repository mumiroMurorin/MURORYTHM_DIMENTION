using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public class NoteData
    {
        ReactiveCollection<float> range = new ReactiveCollection<float>();

        public IReadOnlyReactiveCollection<float> Range { get { return range; } }

        public void SetRange(List<float> range)
        {
            this.range = new ReactiveCollection<float>(range);
        }

        public void AddRange(bool isAddLast)
        {
            float value = isAddLast ? range.Last() + 1 : range[0] - 1;
            range.Insert(isAddLast ? range.Count : 0, value);
        }
    }

    /// <summary>
    /// エディットモード一覧
    /// </summary>
    public enum EditMode
    {
        None,
        Deploy,
        Destroy,
        Move,
        Scale,
    }

    /// <summary>
    /// 音楽再生モード
    /// </summary>
    public enum PlayMode
    {
        Stop,
        Play,
    }

    /// <summary>
    /// 配置ノーツ一覧
    /// </summary>
    public enum DeploymentNoteType
    {
        TouchNote,
        DynamicNoteUpward,
        DynamicNoteRightward,
        DynamicNoteLeftward,
    }
}
