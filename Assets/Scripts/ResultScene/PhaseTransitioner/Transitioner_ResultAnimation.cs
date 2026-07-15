using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TransitionerInResultScene
{
    public class Transitioner_ResultAnimation : IPhaseTransitionerInResultScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimelinePlayer> resultAnimation;
        [SerializeField] MusicDataGetter musicDataGetter;
        [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;

        [Header("Additional Animation")]
        [SerializeField] Transform additionalAnimatorParent;
        [SerializeField] string additionalAnimationStateName;
        [SerializeField] int additionalAnimationLayerIndex;
        [SerializeField] float additionalAnimationTimeoutSeconds = 10f;

        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.ResultAnimation;
        Animator additionalAnimatorInstance;

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("[Transition] Transition to \"Result\"");

            if (resultAnimation == null || resultAnimation.Value == null) { PlayAdditionalAnimationThenTransition(); }
            else { resultAnimation.Value.PlayAnimation(PlayAdditionalAnimationThenTransition); }
        }

        private void PlayAdditionalAnimationThenTransition()
        {
            Animator additionalAnimator = CreateAdditionalAnimator();
            if (additionalAnimator == null)
            {
                TransitionNextPhase();
                return;
            }

            PlayAdditionalAnimationAsync(additionalAnimator).Forget();
        }

        private Animator CreateAdditionalAnimator()
        {
            MusicData musicData = musicDataGetter?.DataGetter?.Music?.Value;
            if (musicData == null) { return null; }

            Animator prefab = symphonyTypePresentationDatabase?.GetResultAdditionalAnimatorPrefab(musicData.SymphonyType);
            if (prefab == null) { return null; }

            if (additionalAnimatorInstance != null)
            {
                Object.Destroy(additionalAnimatorInstance.gameObject);
            }

            Transform parent = additionalAnimatorParent != null ? additionalAnimatorParent : null;
            additionalAnimatorInstance = parent != null ? Object.Instantiate(prefab, parent) : Object.Instantiate(prefab);
            additionalAnimatorInstance.transform.localPosition = Vector3.zero;
            additionalAnimatorInstance.transform.localRotation = Quaternion.identity;
            additionalAnimatorInstance.transform.localScale = Vector3.one;
            return additionalAnimatorInstance;
        }

        private async UniTaskVoid PlayAdditionalAnimationAsync(Animator additionalAnimator)
        {
            additionalAnimator.gameObject.SetActive(true);
            additionalAnimator.enabled = true;

            if (string.IsNullOrEmpty(additionalAnimationStateName))
            {
                additionalAnimator.Rebind();
                additionalAnimator.Update(0f);
            }
            else
            {
                additionalAnimator.Play(additionalAnimationStateName, additionalAnimationLayerIndex, 0f);
            }

            await UniTask.Yield();

            int startStateHash = additionalAnimator.GetCurrentAnimatorStateInfo(additionalAnimationLayerIndex).fullPathHash;
            float elapsedSeconds = 0f;
            while (additionalAnimator != null)
            {
                AnimatorStateInfo stateInfo = additionalAnimator.GetCurrentAnimatorStateInfo(additionalAnimationLayerIndex);
                bool isInTransition = additionalAnimator.IsInTransition(additionalAnimationLayerIndex);
                if (!isInTransition && stateInfo.fullPathHash != startStateHash)
                {
                    break;
                }

                if (!isInTransition &&
                    !stateInfo.loop &&
                    stateInfo.normalizedTime >= 1f)
                {
                    break;
                }

                elapsedSeconds += Time.deltaTime;
                if (additionalAnimationTimeoutSeconds > 0f && elapsedSeconds >= additionalAnimationTimeoutSeconds)
                {
                    Debug.LogWarning("[Transition] Additional ResultAnimation timed out.");
                    break;
                }

                await UniTask.Yield();
            }

            TransitionNextPhase();
        }

        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInResultScene.Result);
        }
    }
}
