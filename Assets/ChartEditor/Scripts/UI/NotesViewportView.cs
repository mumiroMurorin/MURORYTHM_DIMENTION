using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 置けるノーツの一覧 (タッチノート、ダイナミック、ホールドなどのボタンの列)
    ///
    /// 頂点のレイヤー、編集以外の道具 (接続・タイプ変更: 録音 20261118_010533)、ホールドを1本だけ編集している間 (HoldIsolation) は、隠す。
    /// 隠している間は、譜面情報の欄を、この一覧の場所へ寄せる (ChartInfoPresenter)。
    /// </summary>
    public class NotesViewportView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] GameObject obj;
        [Tooltip("置くノーツの選択を外すボタン (レイヤーのボタンの右。録音 20261118_021838)")]
        [SerializeField] Button clearChoiceButton;

        EditMode editMode;
        EditNoteType editNoteType;
        bool isHiddenByIsolation;

        static NotesViewportView instance;

        /// <summary>
        /// 一覧を隠しているとき、その左端 (画面部品の単位。左上からの位置)。出しているときは false
        /// </summary>
        public static bool TryGetHiddenLeft(out float left)
        {
            left = 0f;
            if (instance == null || instance.obj == null || instance.obj.activeSelf) { return false; }

            left = ((RectTransform)instance.obj.transform).anchoredPosition.x;
            return true;
        }

        void Awake()
        {
            instance = this;
            if (clearChoiceButton != null)
            {
                clearChoiceButton.onClick.AddListener(() =>
                {
                    if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
                    ClearChoice();
                });
            }
            // 選んでいないときは、押せない形にする
            DeploymentNoteChoice.OnChanged += RefreshClearButton;
            RefreshClearButton();
        }

        private void RefreshClearButton()
        {
            if (clearChoiceButton != null) { clearChoiceButton.interactable = !DeploymentNoteChoice.IsNone; }
        }

        /// <summary>
        /// 一覧の、ボタンの無い所をダブルクリック: 置くノーツの選択を外す (録音 20261118_015949)
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2) { ClearChoice(); }
        }

        private static void ClearChoice()
        {
            if (DeploymentNoteChoice.IsNone) { return; }
            DeploymentNoteChoice.SetNone(true);
            Debug.Log("【配置】置くノーツの選択を外しました (ノーツを置かず、動かす・選ぶだけになります。一覧のボタンで、また選べます)");
        }

        void OnDestroy()
        {
            DeploymentNoteChoice.OnChanged -= RefreshClearButton;
            if (instance == this) { instance = null; }
        }

        public void OnChangeEditMode(EditMode mode, EditNoteType editNoteType)
        {
            this.editMode = mode;
            this.editNoteType = editNoteType;
            Refresh();
        }

        /// <summary>
        /// ホールドを1本だけ編集している間は、ノーツを置けないので、一覧を隠す (終わったら、元の状態に戻す)
        /// </summary>
        public void SetHiddenByIsolation(bool isHidden)
        {
            isHiddenByIsolation = isHidden;
            Refresh();
        }

        private void Refresh()
        {
            // (最初から隠れていて Awake が呼ばれていなくても、譜面情報の欄が見つけられるように)
            instance = this;
            bool isOtherTool = editMode == EditMode.Connect || editMode == EditMode.Connecting || editMode == EditMode.DisConnect || editMode == EditMode.ChangeType;
            bool isShown = editNoteType != EditNoteType.Vertices && !isOtherTool && !isHiddenByIsolation;
            if (obj.activeSelf != isShown) { obj.SetActive(isShown); }
            // 選択を外すボタンは、一覧が出ているときだけ
            if (clearChoiceButton != null && clearChoiceButton.gameObject.activeSelf != isShown) { clearChoiceButton.gameObject.SetActive(isShown); }
        }
    }

}
