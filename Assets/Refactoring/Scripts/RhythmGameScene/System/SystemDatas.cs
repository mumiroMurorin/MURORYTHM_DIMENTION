using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// �C���Q�[�����̓����X�e�[�^�X�̗񋓌^
    /// </summary>
    public enum PhaseStatusInRhythmGame
    {
        LoadData,
        LoadChart,
        FadeIn,
        LoadBody,
        StartAnimation,
        Play,
        EndAnimation,
        FadeOut,
        TransitionResultScene,
    }
}
