using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ロックしたノーツの右上に、鍵の印を出す (録音 20261116_235216)
    ///
    /// 印の見本はシーンに置いてある (template。隠してある)。ロックしたノーツの数だけ、見本を複製して使う (足りなければ増やす)。
    /// グラウンド・スペースのレイヤーの間だけ、レーンを映しているカメラに映っているノーツに出す。
    /// </summary>
    public class NoteLockIconsView : MonoBehaviour
    {
        [Tooltip("鍵の印の見本 (隠しておく)")]
        [SerializeField] Image template;
        [Tooltip("ノーツの右上の角から、印の中心をどれだけずらすか (画面部品の単位)")]
        [SerializeField] Vector2 offset = new Vector2(2f, 2f);

        readonly List<Image> icons = new List<Image>();
        readonly List<NoteObject> lockedObjects = new List<NoteObject>();
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly Vector3[] corners = new Vector3[8];
        Canvas canvas;

        void LateUpdate()
        {
            int used = 0;
            Camera camera = EditorViewCameras.GroundView;

            if (template != null && camera != null && NoteLock.HasAny && NoteLock.IsIconLayer)
            {
                canvas ??= GetComponentInParent<Canvas>();
                float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

                NoteLock.CollectLockedObjects(lockedObjects);
                foreach (var noteObject in lockedObjects)
                {
                    if (!TryGetTopRight(noteObject, camera, out Vector2 screen)) { continue; }

                    if (used >= icons.Count) { icons.Add(CreateIcon()); }
                    Image icon = icons[used++];
                    icon.rectTransform.anchoredPosition = screen / scale + offset;
                    if (!icon.gameObject.activeSelf) { icon.gameObject.SetActive(true); }
                }
            }

            for (int i = used; i < icons.Count; i++)
            {
                if (icons[i].gameObject.activeSelf) { icons[i].gameObject.SetActive(false); }
            }
        }

        /// <summary>
        /// ノーツの絵の、画面の上での右上の角 (カメラに映っていなければ false)
        /// (次のノーツまでの帯は含めない: 帯を含めると、ロックした中継点の印が、次の節の所に出てしまう。録音 20261117_012356)
        /// </summary>
        private bool TryGetTopRight(NoteObject noteObject, Camera camera, out Vector2 screen)
        {
            screen = default;

            // レーンを映している範囲から大きく外れたノーツは、絵を集める前に飛ばす (たくさんロックしたときに重くならないように)
            Vector3 viewport = camera.WorldToViewportPoint(noteObject.transform.position);
            if (viewport.z < 0f || viewport.x < -0.5f || viewport.x > 1.5f || viewport.y < -0.5f || viewport.y > 1.5f) { return false; }

            NoteLock.CollectBodyRenderers(noteObject, renderers);

            bool hasBounds = false;
            Bounds bounds = default;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || !renderer.isVisible) { continue; }
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else { bounds.Encapsulate(renderer.bounds); }
            }
            renderers.Clear();
            if (!hasBounds) { return false; }

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            corners[0] = new Vector3(min.x, min.y, min.z);
            corners[1] = new Vector3(max.x, min.y, min.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(max.x, max.y, min.z);
            corners[4] = new Vector3(min.x, min.y, max.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(min.x, max.y, max.z);
            corners[7] = new Vector3(max.x, max.y, max.z);

            float right = float.MinValue;
            float top = float.MinValue;
            foreach (var corner in corners)
            {
                Vector3 point = camera.WorldToScreenPoint(corner);
                if (point.z < 0f) { return false; }
                right = Mathf.Max(right, point.x);
                top = Mathf.Max(top, point.y);
            }

            // レーンを映している範囲の外には出さない
            Rect area = camera.pixelRect;
            if (right < area.xMin || right > area.xMax + 8f || top < area.yMin || top > area.yMax + 8f) { return false; }

            screen = new Vector2(right, top);
            return true;
        }

        private Image CreateIcon()
        {
            Image icon = Instantiate(template, template.transform.parent);
            icon.name = "LockIcon";
            icon.raycastTarget = false;
            return icon;
        }
    }
}
