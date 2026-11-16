using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 欄 (ログ欄・操作説明・クイックメニュー) を、ダブルクリックで小さいマスにしまい、マスをクリックすると元に戻す
    ///
    /// ・欄の上の、ボタンや入力欄ではない所 (背景・文字) をダブルクリックすると、欄が消えて、欄の角に小さいマスが出る
    /// ・マスをクリックすると、欄が元どおりに出る (マスは消える)
    /// ・しまっているかどうかは覚えておき、次に起動したときも同じにする (PlayerPrefs)
    ///
    /// 欄は、消すとき非表示 (SetActive) にせず、透明にしてクリックも通さないようにするだけにする。
    /// (欄を出す・出さないは、ほかの部品も決めている。操作説明は出す行が無いとき、クイックメニューはダイアログを開いている間に、
    ///  非表示になる。非表示を使うと、それとぶつかる。また、ログ欄は、隠している間もログを受け取り続ける必要がある)
    /// マスは、欄が非表示になっている間は出さない (出す行が無い・ダイアログを開いている、など)。
    ///
    /// このコンポーネントは、欄とマスの両方の親 (いつも出ている、画面いっぱいの入れ物) の中に置く。
    /// 表示メニューで欄を隠したときは、親ごと隠れるので、マスも一緒に隠れる。
    /// </summary>
    public class MinimizableWindow : MonoBehaviour
    {
        [Tooltip("しまう欄 (ここの背景をダブルクリックすると、しまう)")]
        [SerializeField] RectTransform window;
        [Tooltip("しまっている間に出す、小さいマス (クリックすると、欄を元に戻す)")]
        [SerializeField] Button minimizedButton;
        [Tooltip("しまっているかどうかを保存するときの名前 (欄ごとに違う名前にする)")]
        [SerializeField] string prefsKey;
        [Tooltip("ここの上のダブルクリックでは、しまわない (ミニプレビューの映像など、クリックをほかの操作に使う所)")]
        [SerializeField] RectTransform[] ignoredAreas;

        // 保存するときの名前 (後ろに prefsKey が付く)
        const string PREFS_KEY_PREFIX = "ChartEditor.WindowMinimized.";

        bool isMinimized;
        CanvasGroup windowGroup;

        /// <summary>欄をしまっているか</summary>
        public bool IsMinimized => isMinimized;

        /// <summary>しまっている間に出す、小さいマス (実行時に作る欄が、同じ見た目のマスを複製するのに使う)</summary>
        public Button MinimizedButton => minimizedButton;

        /// <summary>
        /// 実行時にコードで作った欄に付けるとき、欄とマスを渡す (AddComponent の直後に呼ぶ)
        /// (Awake は AddComponent の中で、欄が決まる前に動くので、ここで欄のダブルクリックとマスのクリックをつなぐ)
        /// </summary>
        public void Setup(RectTransform window, Button minimizedButton, string prefsKey)
        {
            this.window = window;
            this.minimizedButton = minimizedButton;
            this.prefsKey = prefsKey;

            if (window != null)
            {
                var relay = window.GetComponent<WindowDoubleClickRelay>();
                if (relay == null) { relay = window.gameObject.AddComponent<WindowDoubleClickRelay>(); }
                relay.OnDoubleClicked -= Minimize;
                relay.OnDoubleClicked += Minimize;
            }
            if (minimizedButton != null)
            {
                minimizedButton.onClick.RemoveListener(Restore);
                minimizedButton.onClick.AddListener(Restore);
            }
            RefreshButton();
        }

        private void Awake()
        {
            if (window != null)
            {
                // 欄のダブルクリックを受け取る部品を、欄に付ける (ボタンなどの上のクリックは、ボタンが受け取るので、ここには来ない)
                var relay = window.GetComponent<WindowDoubleClickRelay>();
                if (relay == null) { relay = window.gameObject.AddComponent<WindowDoubleClickRelay>(); }
                relay.IgnoredAreas = ignoredAreas;
                relay.OnDoubleClicked += Minimize;
            }

            if (minimizedButton != null) { minimizedButton.onClick.AddListener(Restore); }
        }

        private void Start()
        {
            // 前回、しまったまま終えていれば、しまった状態で始める
            isMinimized = !string.IsNullOrEmpty(prefsKey) && PlayerPrefs.GetInt(PREFS_KEY_PREFIX + prefsKey, 0) != 0;
            Apply();
        }

        private void LateUpdate()
        {
            // 欄が非表示になったり、また出たりするので、毎フレーム合わせる
            RefreshButton();
        }

        /// <summary>
        /// 欄をしまって、小さいマスにする
        /// </summary>
        public void Minimize()
        {
            SetMinimized(true);
        }

        /// <summary>
        /// 欄を元に戻す
        /// </summary>
        public void Restore()
        {
            SetMinimized(false);
        }

        private void SetMinimized(bool value)
        {
            if (isMinimized == value) { return; }

            isMinimized = value;
            Apply();

            if (!string.IsNullOrEmpty(prefsKey))
            {
                PlayerPrefs.SetInt(PREFS_KEY_PREFIX + prefsKey, isMinimized ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private void Apply()
        {
            if (window != null)
            {
                if (windowGroup == null)
                {
                    windowGroup = window.GetComponent<CanvasGroup>();
                    // しまったことが無い欄には、何も足さない
                    if (windowGroup == null && isMinimized) { windowGroup = window.gameObject.AddComponent<CanvasGroup>(); }
                }

                if (windowGroup != null)
                {
                    windowGroup.alpha = isMinimized ? 0f : 1f;
                    windowGroup.blocksRaycasts = !isMinimized;
                    windowGroup.interactable = !isMinimized;
                }
            }

            RefreshButton();
        }

        /// <summary>
        /// 小さいマスを、しまっていて、欄が非表示になっていないときだけ出す
        /// </summary>
        private void RefreshButton()
        {
            if (minimizedButton == null) { return; }

            bool isShown = isMinimized && window != null && window.gameObject.activeSelf;
            if (minimizedButton.gameObject.activeSelf != isShown) { minimizedButton.gameObject.SetActive(isShown); }
        }
    }
}
