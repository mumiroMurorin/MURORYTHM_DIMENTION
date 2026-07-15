using UnityEngine;

namespace UIInGameOverScene
{
    public class CharacterAnimationControllersBinder : MonoBehaviour
    {
        [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;
        [SerializeField] MusicDataGetter musicDataGetter;
        [SerializeField] Transform characterAnimationControllerParent;

        CharacterAnimationController currentController;

        private void Start()
        {
            CreateCharacterAnimationController();
        }

        public void OnContinueSelected()
        {
            EnsureController();
            currentController?.OnContinueSelected();
        }

        public void OnFinishSelected()
        {
            EnsureController();
            currentController?.OnFinishSelected();
        }

        private void EnsureController()
        {
            if (currentController != null) { return; }

            CreateCharacterAnimationController();
        }

        private void CreateCharacterAnimationController()
        {
            MusicData musicData = musicDataGetter?.DataGetter?.Music?.Value;

            CharacterAnimationController prefab = symphonyTypePresentationDatabase?.GetCharacterAnimationControllerPrefab(musicData == null ? SymphonyType.None : musicData.SymphonyType);
            if (prefab == null)
            {
                Debug.LogWarning($"[CharacterAnimationControllersBinder] CharacterAnimationController prefab is not set: {musicData.SymphonyType}");
                return;
            }

            if (currentController != null)
            {
                Destroy(currentController.gameObject);
            }

            Transform parent = characterAnimationControllerParent != null ? characterAnimationControllerParent : transform;
            currentController = Instantiate(prefab, parent);
            //currentController.transform.localPosition = Vector3.zero;
            currentController.transform.localRotation = Quaternion.identity;
            currentController.transform.localScale = Vector3.one;
        }
    }
}
