using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 小節・分線の設定ダイアログを開いている間、その線の、レーンの上の表示 (小節の情報、分線の BPM の欄) を囲む枠
    ///
    /// 「今どの小節・どの分線の設定を開いているか」が、ひと目で分かるようにする。
    /// 枠は、シーン(EditorCanvas)に最初から置いてある。ここでは、クリックされた表示の当たり判定が、
    /// 画面の上で占めている範囲に、枠を合わせるだけを行う (開いている間に、譜面がスクロールしても付いていく)。
    /// 枠は、クリックを受け取らない。
    /// </summary>
    public class RhythmConfigTargetFrameView : MonoBehaviour
    {
        [Tooltip("枠 (位置と大きさを、囲む相手に合わせる)")]
        [SerializeField] RectTransform frame;
        [Tooltip("枠を、囲む相手よりどれだけ外側に出すか (画面のピクセル数)")]
        [SerializeField] float padding = 4f;

        // 囲む相手 (レーンの上の表示の当たり判定)
        Collider target;

        // 当たり判定の箱の、8つの角 (画面の上の範囲を求めるのに使う)
        static readonly Vector3[] cornerSigns =
        {
            new Vector3(-1f, -1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, -1f),
            new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(-1f, 1f, 1f), new Vector3(1f, 1f, 1f),
        };

        /// <summary>
        /// 枠を出す
        /// </summary>
        /// <param name="target">囲む相手 (null のときは、何も出さない)</param>
        public void Show(Collider target)
        {
            this.target = target;
            Refresh();
        }

        /// <summary>
        /// 枠を隠す
        /// </summary>
        public void Hide()
        {
            target = null;
            SetFrameActive(false);
        }

        private void LateUpdate()
        {
            if (target != null) { Refresh(); }
        }

        /// <summary>
        /// 枠の位置と大きさを、囲む相手の、今の画面上の範囲に合わせる
        /// </summary>
        private void Refresh()
        {
            if (frame == null) { return; }

            Camera camera = EditorViewCameras.GroundView;
            // 相手が無くなった (譜面を作り直したときなど)、または隠れている
            if (target == null || !target.gameObject.activeInHierarchy || camera == null)
            {
                SetFrameActive(false);
                return;
            }

            // 当たり判定の箱の角を、画面の上の位置に直して、それらを囲む四角を求める
            Bounds bounds = target.bounds;
            // 当たり判定が無効になっている間は、箱の大きさが分からない
            if (bounds.size.sqrMagnitude < 0.00000001f)
            {
                SetFrameActive(false);
                return;
            }
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 sign in cornerSigns)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, sign);
                Vector3 screen = camera.WorldToScreenPoint(corner);
                // カメラの後ろにあるものは、囲めない
                if (screen.z < 0f)
                {
                    SetFrameActive(false);
                    return;
                }

                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }

            min -= new Vector2(padding, padding);
            max += new Vector2(padding, padding);

            SetFrameActive(true);

            // このキャンバスは画面にそのまま重なっているので、position が画面上の位置になる (枠の基準は、左下の角)
            frame.position = min;
            float scale = frame.lossyScale.x;
            frame.sizeDelta = scale > 0f ? (max - min) / scale : Vector2.zero;
        }

        private void SetFrameActive(bool isActive)
        {
            if (frame != null && frame.gameObject.activeSelf != isActive) { frame.gameObject.SetActive(isActive); }
        }
    }
}
