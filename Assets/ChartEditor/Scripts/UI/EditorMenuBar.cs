using System;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 画面上のメニューバー (「ファイル」「編集」などの見出しと、その下に開く一覧)
    ///
    /// 見出し・一覧・項目は、すべてシーン(MenuCanvas)に最初から置いてある。ここでは、開く・閉じるだけを扱う。
    /// ・見出しを押すと、その一覧を開く。もう一度押すと閉じる
    /// ・一覧を開いている間は、ほかの見出しにカーソルを乗せるだけで、そちらの一覧に切り替わる
    /// ・メニューの外を押すか、Esc キーで閉じる
    /// 項目を押したときの操作は、EditorMenuPresenter が行う。
    /// </summary>
    public class EditorMenuBar : MonoBehaviour
    {
        [SerializeField] EditorMenu[] menus;
        [Tooltip("メニューを開いている間だけ表示する、画面全体を覆う見えない板")]
        [SerializeField] EditorMenuOutsideArea outsideArea;

        // 今、一覧を開いているメニューバー (シーンに1つ)
        static EditorMenuBar openMenuBar;
        // 最後に一覧を閉じたフレーム
        static int lastClosedFrame = -1;
        // メニューバーを置いてあるキャンバス (MenuCanvas。いつも画面のいちばん手前に出す画面部品を、ここに置いている)
        static Transform menuCanvasRoot;

        EditorMenu openMenu;
        bool isInteractable = true;
        // 前のフレームの終わりに、入力欄に文字を打っている最中だったか
        bool wasTyping;

        /// <summary>
        /// メニューの裏にある譜面・プレビューを、マウスで操作させないようにするか
        ///
        /// 一覧を開いている間と、閉じたフレームの間。
        /// (メニューの外を押して閉じたとき、同じフレームのうちに、その押下でノーツが掴まれたりしないように。
        ///  閉じる処理と譜面の操作のどちらが先に動くかは決まっていないので、閉じたフレームいっぱいは止めておく)
        /// </summary>
        public static bool IsBlockingChartInput => openMenuBar != null || Time.frameCount == lastClosedFrame;

        /// <summary>
        /// メニューバーと同じキャンバス (MenuCanvas) に置いてある画面部品か
        /// (メニューバー、開いた一覧、譜面情報の欄。全画面プレビューでは、これらの上のクリックを、プレビューへの操作として扱わない)
        /// </summary>
        public static bool IsOnMenuCanvas(GameObject target)
        {
            return menuCanvasRoot != null && target != null && target.transform.root == menuCanvasRoot;
        }

        /// <summary>
        /// 一覧を開いたとき (項目の状態を、開いた時点のものに合わせ直すのに使う)
        /// </summary>
        public Action OnOpenedListener { get; set; }

        public bool IsOpen => openMenu != null;

        private void Awake()
        {
            menuCanvasRoot = transform.root;

            if (menus != null)
            {
                foreach (var menu in menus)
                {
                    if (menu == null) { continue; }

                    menu.OnPressedListener += OnMenuPressed;
                    menu.OnEnteredListener += OnMenuEntered;
                    menu.SetOpen(false);
                }
            }

            if (outsideArea != null)
            {
                outsideArea.OnPressedListener += Close;
                outsideArea.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Esc で閉じる。メニューの中の入力欄に文字を打っている間の Esc は、入力の取り消しに使う
            // (入力欄が先に Esc を受け取ると、ここへ来たときにはもう入力が終わっている。前のフレームの状態も見て判断する)
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape) && !InputFieldFocus.IsTyping && !wasTyping) { Close(); }
        }

        private void LateUpdate()
        {
            wasTyping = InputFieldFocus.IsTyping;
        }

        private void OnDisable()
        {
            // メニューバーごと隠されるとき (全画面プレビューへ移るときなど) は、開いている一覧も閉じる
            Close();
        }

        private void OnDestroy()
        {
            if (openMenuBar == this) { openMenuBar = null; }
            if (menuCanvasRoot == transform.root) { menuCanvasRoot = null; }
        }

        /// <summary>
        /// 見出しを押せるかどうかを切り替える。押せなくするときは、開いている一覧も閉じる
        /// </summary>
        public void SetInteractable(bool isInteractable)
        {
            if (this.isInteractable == isInteractable) { return; }

            this.isInteractable = isInteractable;
            if (!isInteractable) { Close(); }

            if (menus == null) { return; }
            foreach (var menu in menus)
            {
                if (menu != null) { menu.SetInteractable(isInteractable); }
            }
        }

        /// <summary>
        /// 開いている一覧を閉じる (開いていなければ何もしない)
        /// </summary>
        public void Close()
        {
            if (openMenu == null) { return; }

            openMenu.SetOpen(false);
            openMenu = null;

            if (outsideArea != null) { outsideArea.gameObject.SetActive(false); }

            if (openMenuBar == this) { openMenuBar = null; }
            lastClosedFrame = Time.frameCount;
        }

        private void Open(EditorMenu menu)
        {
            if (menu == null || menu == openMenu || !isInteractable) { return; }

            if (openMenu != null) { openMenu.SetOpen(false); }

            openMenu = menu;
            openMenu.SetOpen(true);

            if (outsideArea != null) { outsideArea.gameObject.SetActive(true); }
            openMenuBar = this;

            OnOpenedListener?.Invoke();
        }

        private void OnMenuPressed(EditorMenu menu)
        {
            // 開いている見出しをもう一度押したら閉じる
            if (menu == openMenu) { Close(); }
            else { Open(menu); }
        }

        private void OnMenuEntered(EditorMenu menu)
        {
            // どれかの一覧を開いている間だけ、カーソルを乗せた見出しの一覧に切り替える
            if (openMenu != null && menu != openMenu) { Open(menu); }
        }
    }
}
