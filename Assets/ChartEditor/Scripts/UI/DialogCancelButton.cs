using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 設定の小窓(小節の設定・分線の設定)に、Apply の隣の Cancel ボタンを付ける
    ///
    /// 今ある Apply ボタンを実行時に複製して作るので、見た目は Apply と同じになる。
    /// (シーンを直接編集せずにボタンを増やすための仕組み)
    /// </summary>
    public static class DialogCancelButton
    {
        // 複製元にするボタンと、作るオブジェクトの名前
        const string APPLY_BUTTON_NAME = "ApplyButton";
        const string CANCEL_BUTTON_NAME = "CancelButton";
        const string BUTTON_ROW_NAME = "Buttons";
        const string CANCEL_TEXT = "Cancel";
        // 2つのボタンの間の隙間
        const float BUTTON_GAP = 20f;

        /// <param name="dialog">小窓 (この下から Apply ボタンを探す)</param>
        /// <param name="onCancel">Cancel が押されたときの処理</param>
        /// <returns>作った Cancel ボタン。Apply ボタンが見つからなかったときは null</returns>
        public static Button Create(Transform dialog, Action onCancel)
        {
            if (dialog == null) { return null; }

            Button applyButton = null;
            foreach (var button in dialog.GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name != APPLY_BUTTON_NAME) { continue; }

                applyButton = button;
                break;
            }

            if (applyButton == null) { return null; }

            var applyRect = (RectTransform)applyButton.transform;
            Vector2 buttonSize = applyRect.rect.size;

            // 小窓の中身は、上から順に縦に並べる仕組み(VerticalLayoutGroup)で置かれているので、
            // Apply の隣に直接ボタンを足すと、Apply の下の段に並んでしまう。
            // そこで、Apply のあった段に「ボタンを横に並べる入れ物」を置き、その中に Apply と Cancel を入れる
            var rowObject = new GameObject(BUTTON_ROW_NAME, typeof(RectTransform));
            rowObject.layer = applyButton.gameObject.layer;

            var rowRect = (RectTransform)rowObject.transform;
            rowRect.SetParent(applyRect.parent, false);
            rowRect.SetSiblingIndex(applyRect.GetSiblingIndex());
            // 入れ物の位置は、Apply と同じ基準で決めておく (縦に並べる仕組みが無い場合でも、Apply のあった場所に来る)
            rowRect.anchorMin = applyRect.anchorMin;
            rowRect.anchorMax = applyRect.anchorMax;
            rowRect.pivot = applyRect.pivot;
            rowRect.anchoredPosition = applyRect.anchoredPosition;
            rowRect.sizeDelta = new Vector2(buttonSize.x * 2f + BUTTON_GAP, buttonSize.y);

            // Apply を入れ物の左側へ移す (ボタンに設定されている「押されたときの処理」は、そのまま残る)
            applyRect.SetParent(rowRect, false);
            PlaceInRow(applyRect, buttonSize, -1f);

            // Cancel を右側に作る
            GameObject cancelObject = UnityEngine.Object.Instantiate(applyButton.gameObject, rowRect);
            cancelObject.name = CANCEL_BUTTON_NAME;
            PlaceInRow((RectTransform)cancelObject.transform, buttonSize, 1f);

            foreach (var text in cancelObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = CANCEL_TEXT;
            }

            // 複製元から引き継いだ「Apply が押されたときの処理」を外して、Cancel の処理に付け替える
            var cancelButton = cancelObject.GetComponent<Button>();
            cancelButton.onClick = new Button.ButtonClickedEvent();
            cancelButton.onClick.AddListener(() =>
            {
                // 押したボタンが選択中のまま残らないようにする (残ると、Space キーや Enter キーでもう一度押されてしまう)
                if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }

                onCancel?.Invoke();
            });

            return cancelButton;
        }

        /// <summary>
        /// ボタンを、入れ物の中の左側(side = -1)または右側(side = 1)に置く
        /// </summary>
        private static void PlaceInRow(RectTransform buttonRect, Vector2 buttonSize, float side)
        {
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = buttonSize;
            buttonRect.anchoredPosition = new Vector2(side * (buttonSize.x + BUTTON_GAP) * 0.5f, 0f);
        }
    }
}
