using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace UIInSelectScene
{
    public class BackGroundControllerView : MonoBehaviour
    {
        [SerializeField] float changeDuration = 0.5f;
        [SerializeField] DoShakeBuilder doShakeBuilder;
        [SerializeField] GameObject backGroundObj;
        [SerializeField] Image backGroundImage;

        private Tweener shakeTween;

        private void Start()
        {
            // 常にちょっと揺らすか
            doShakeBuilder.ApplyShake(backGroundObj.transform);
        }

        public void OnChangeMusicTopic(MusicData musicData)
        {
            if(backGroundImage == null) { return; }

            // フェードアウト（透明にする）
            backGroundImage.DOColor(Color.black, changeDuration / 2).OnComplete(() =>
            {
                // スプライトを変更
                backGroundImage.sprite = musicData.ThemeSprite;

                // フェードイン（元の透明度に戻す）
                backGroundImage.DOColor(Color.white, changeDuration / 2);
            });
        }

        private void OnDestroy()
        {
            doShakeBuilder.Kill();
        }
    }
    
}