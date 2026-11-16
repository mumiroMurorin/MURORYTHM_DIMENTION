using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ショートカット一覧のウィンドウ (メニューの「ヘルプ」→「ショートカット一覧」。2026-11-17)
    ///
    /// 譜面エディタのキー・マウスの操作を、場面ごとにまとめて、全部を一度に見せる
    /// (操作説明のウィンドウは、そのときに使える操作しか出さないので、全体はここで見る)。
    /// 中身の文はシーンに置いてある (生成スクリプトが書く)。ここは出し入れだけをする。
    /// 外側 (暗くした所) をクリック、× のボタン、Esc で閉じる。開いている間は、裏の譜面の操作を止める (EditorMenuBar)。
    /// </summary>
    public class ShortcutListView : MonoBehaviour
    {
        [Tooltip("ウィンドウと、その外側の暗くした所 (隠しておく)")]
        [SerializeField] GameObject root;
        [SerializeField] Button closeButton;
        [Tooltip("外側の暗くした所 (押すと閉じる)")]
        [SerializeField] Button backgroundButton;

        static ShortcutListView instance;
        static int lastClosedFrame = -1;

        /// <summary>開いているか</summary>
        public static bool IsOpen => instance != null && instance.root != null && instance.root.activeSelf;

        /// <summary>開いているか、このフレームに閉じたか (閉じた Esc やクリックが、裏の操作に届かないように)</summary>
        public static bool IsOpenOrJustClosed => IsOpen || Time.frameCount == lastClosedFrame;

        void Awake()
        {
            instance = this;
            if (closeButton != null) { closeButton.onClick.AddListener(Hide); }
            if (backgroundButton != null) { backgroundButton.onClick.AddListener(Hide); }
        }

        void OnDestroy()
        {
            if (instance == this) { instance = null; }
        }

        void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) { Hide(); }
        }

        public void Show()
        {
            if (root != null && !root.activeSelf) { root.SetActive(true); }
        }

        public void Hide()
        {
            if (root == null || !root.activeSelf) { return; }
            root.SetActive(false);
            lastClosedFrame = Time.frameCount;
        }
    }
}
