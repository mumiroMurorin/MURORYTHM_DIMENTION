using UnityEngine;
using UnityEngine.UI;

namespace UIInResultScene
{
    public class ResultCircleView : MonoBehaviour
    {
        [SerializeField] Image jacketImage;
        [SerializeField] string fallbackJacketObjectName = "Jacket";

        public void SetJacket(Sprite sprite)
        {
            if (sprite == null) { return; }

            Image targetImage = GetJacketImage();
            if (targetImage == null)
            {
                Debug.LogWarning("[ResultCircleView] Jacket Image is not found.");
                return;
            }

            targetImage.sprite = sprite;
        }

        private Image GetJacketImage()
        {
            if (jacketImage != null) { return jacketImage; }

            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image != null && image.gameObject.name == fallbackJacketObjectName)
                {
                    jacketImage = image;
                    return jacketImage;
                }
            }

            return null;
        }
    }
}
