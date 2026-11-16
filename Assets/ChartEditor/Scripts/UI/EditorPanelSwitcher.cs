using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ChartEditor
{
    /// <summary>
    /// 画面上のメニューで切り替えた「表示する / しない」を、画面部品に反映し、次に起動したときのために覚えておく
    ///
    /// ・「表示」のログ欄と、「クイックメニュー」の各セクション (左下の設定欄の行) を、表示したり隠したりする
    ///   (譜面情報と全体マップは、それぞれの Presenter が、編集中のレイヤーと合わせて決めている)
    /// ・クイックメニューは、編集画面でも全画面プレビューでも、同じものを出す (小節・分線の設定ダイアログや、説明書を開いている間は隠す)
    /// ・表示する / しないの設定を、PlayerPrefs に保存し、起動時に読み込む
    /// 対象の画面部品は、シーンに置いてあるものを、panels で指定する。
    /// </summary>
    public class EditorPanelSwitcher : MonoBehaviour
    {
        [Serializable]
        class PanelTarget
        {
            public EditorPanel panel;
            [Tooltip("表示したり隠したりする画面部品")]
            public Transform[] targets;
            [Tooltip("隠すとき、非表示(SetActive)にせず、透明にするだけにする (隠している間も、中の処理を動かし続けたい部品用)")]
            public bool isHiddenByTransparency;
        }

        [SerializeField] PanelTarget[] panels;
        [Tooltip("クイックメニュー (左下の設定欄) 全体。表示するセクションが1つも無いときは、枠ごと隠す")]
        [SerializeField] GameObject quickMenu;
        [Tooltip("クイックメニューの中身 (セクションを縦に並べている部品)。枠の高さを、この高さに合わせる")]
        [SerializeField] RectTransform quickMenuContent;
        [Tooltip("クイックメニューの枠の高さの上限。中身がこれより高いときは、枠の中をスクロールして見る")]
        [SerializeField] float quickMenuMaxHeight = 440f;

        // 保存するときの名前 (後ろに EditorPanel の番号が付く)
        // (v2: クイックメニューの最初の表示を「よく使うものだけ」に変えたときに、名前を変えた。前の名前で保存された設定は使わない)
        const string PREFS_KEY_PREFIX = "ChartEditor.PanelVisible.v2.";

        // クイックメニューを隠すモード (小節・分線の設定ダイアログや、説明書を開いている間)
        // (クイックメニューは、全画面プレビューでも出したままにするため、いつも手前に出すキャンバス (MenuCanvas) に置いてある。
        //  隠さないと、ダイアログや説明書の上に重なってしまう)
        static readonly EditMode[] quickMenuHiddenEditModes =
        {
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
        };

        IChartEditorOptionGetter optionGetter;
        IChartEditorOptionSetter optionSetter;
        IChartEditorDataGetter dataGetter;

        [Inject]
        public void Construct(IChartEditorOptionGetter optionGetter, IChartEditorOptionSetter optionSetter, IChartEditorDataGetter dataGetter)
        {
            this.optionGetter = optionGetter;
            this.optionSetter = optionSetter;
            this.dataGetter = dataGetter;
        }

        private void Start()
        {
            if (optionGetter == null || optionSetter == null) { return; }

            // 前回の設定を読み込む (保存されていないものは、部品ごとに決まっている最初の状態にする)
            foreach (EditorPanel panel in Enum.GetValues(typeof(EditorPanel)))
            {
                int defaultValue = panel.IsVisibleByDefault() ? 1 : 0;
                optionSetter.SetPanelVisible(panel, PlayerPrefs.GetInt(GetPrefsKey(panel), defaultValue) != 0);
            }

            foreach (EditorPanel panel in Enum.GetValues(typeof(EditorPanel)))
            {
                EditorPanel target = panel;

                optionGetter.GetPanelVisibility(target)
                    .Subscribe(isVisible =>
                    {
                        Apply(target, isVisible);
                        if (target.IsQuickMenuSection()) { RefreshQuickMenu(); }
                    })
                    .AddTo(this);

                // 切り替えられたら保存する (読み込んだ直後の値は、保存し直さない)
                optionGetter.GetPanelVisibility(target)
                    .Skip(1)
                    .Subscribe(isVisible =>
                    {
                        PlayerPrefs.SetInt(GetPrefsKey(target), isVisible ? 1 : 0);
                        PlayerPrefs.Save();
                    })
                    .AddTo(this);
            }

            // ダイアログや説明書を開いている間は、クイックメニューを隠す
            dataGetter?.CurrentEditMode
                .Subscribe(_ => RefreshQuickMenu())
                .AddTo(this);
        }

        private void LateUpdate()
        {
            FitQuickMenuHeight();
        }

        /// <summary>
        /// クイックメニューの枠の高さを、中身の高さに合わせる (上限まで)
        /// 表示するセクションが少ないときに、枠の下に空きができないようにする
        /// </summary>
        private void FitQuickMenuHeight()
        {
            if (quickMenu == null || quickMenuContent == null || !quickMenu.activeInHierarchy) { return; }

            var frame = (RectTransform)quickMenu.transform;

            // 中身の高さがまだ計算されていないとき (起動した直後、隠していた枠を出し直した直後) は、先に計算させる
            // (0 のまま合わせると、枠が1フレームだけつぶれて見える)
            if (quickMenuContent.rect.height <= 0f) { LayoutRebuilder.ForceRebuildLayoutImmediate(quickMenuContent); }
            if (quickMenuContent.rect.height <= 0f) { return; }

            float height = Mathf.Min(quickMenuContent.rect.height, quickMenuMaxHeight);
            if (Mathf.Abs(frame.sizeDelta.y - height) < 0.5f) { return; }

            frame.sizeDelta = new Vector2(frame.sizeDelta.x, height);
        }

        private static string GetPrefsKey(EditorPanel panel)
        {
            return PREFS_KEY_PREFIX + (int)panel;
        }

        /// <summary>
        /// 画面部品を、表示する / 隠す
        /// </summary>
        private void Apply(EditorPanel panel, bool isVisible)
        {
            if (panels == null) { return; }

            foreach (var entry in panels)
            {
                if (entry == null || entry.panel != panel || entry.targets == null) { continue; }

                foreach (var target in entry.targets)
                {
                    if (target == null) { continue; }

                    if (entry.isHiddenByTransparency) { SetTransparent(target.gameObject, !isVisible); }
                    else if (target.gameObject.activeSelf != isVisible) { target.gameObject.SetActive(isVisible); }
                }
            }
        }

        /// <summary>
        /// 透明にして、クリックも受け付けないようにする (元に戻すときは、見えるようにして、クリックも受け付ける)
        /// </summary>
        private static void SetTransparent(GameObject target, bool isTransparent)
        {
            // (全画面プレビューの再生中にも、同じ部品を隠すことがあるので、隠す理由ごとにまとめて決める)
            UiHideReasons.Set(target, UiHideReasons.PANEL, isTransparent);
        }

        /// <summary>
        /// クイックメニューの枠を、表示するセクションが1つでもあるときだけ出す
        /// (ダイアログや説明書を開いている間は、セクションがあっても出さない)
        /// </summary>
        private void RefreshQuickMenu()
        {
            if (quickMenu == null) { return; }

            if (dataGetter != null && dataGetter.CurrentEditMode.Value.IsInEditModeList(quickMenuHiddenEditModes))
            {
                if (quickMenu.activeSelf) { quickMenu.SetActive(false); }
                return;
            }

            bool hasVisibleSection = false;
            foreach (EditorPanel panel in Enum.GetValues(typeof(EditorPanel)))
            {
                if (panel.IsQuickMenuSection() && optionGetter.GetPanelVisibility(panel).Value)
                {
                    hasVisibleSection = true;
                    break;
                }
            }

            if (quickMenu.activeSelf != hasVisibleSection) { quickMenu.SetActive(hasVisibleSection); }
        }
    }
}
