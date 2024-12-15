using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class ChartGenerator : MonoBehaviour
    {
        [SerializeField] Transform startPoint;
        [SerializeField] Transform judgePoint;

        [SerializeField] GroundTouchNotesFactory groundTouchNotesFactory;

        List<GroundTouchNoteData> groundTouchNoteDatas;
        List<NoteObject> noteList;


        INoteSpawnDataOptionHolder option;

        [Inject]
        public void Inject(INoteSpawnDataOptionHolder option)
        {
            this.option = option;
        }

        public void GenerateNote()
        {
            groundTouchNotesFactory.GenerateGroundTouchNote(groundTouchNoteDatas);
        }


        public void SetNotePosition()
        {
            foreach(GroundTouchNoteData data in groundTouchNoteDatas)
            {
                Vector3 spawnPos = new Vector3(data.SpawnPosition.x, data.SpawnPosition.y, Mathf.Min(startPoint.position.z, data.JudgeTime * option.NoteSpeed));

                //data.NoteGameObject.SetPosition(spawnPos);
            }
        }

    }

    /// <summary>
    /// タッチノーツ(Ground,Slider判定)のFactoryClass
    /// </summary>
    [System.Serializable]
    public class TouchNoteFactory
    {
        [SerializeField] SerializeInterface<ITouchNoteGenerator> generator;

        SliderJudgableDataSet sliderJudgableDataSet;

        /// <summary>
        /// ノーツ生成
        /// </summary>
        /// <returns></returns>
        public NoteObject GenerateTouchNote(IGroundNoteGenerationData generationData, INoteSpawnData spawnData)
        {
            if (sliderJudgableDataSet == null) { Debug.LogWarning($"SliderJudgableDataSetがセットされていません"); }

            var note = generator.Value.GenerateNote(generationData, sliderJudgableDataSet);
            spawnData.NoteGameObject = note.gameObject;

            return note;
        }
    }

    /// <summary>
    /// ダイナミックノーツ(Ground,Space判定)のFactoryClass
    /// </summary>
    [System.Serializable]
    public class GroundDynamicNoteFactory
    {
        [SerializeField] SerializeInterface<IGroundDynamicNoteGenerator> generator;

        SpaceJudgableDataSet spaceJudgableDataSet;

        /// <summary>
        /// ノーツ生成
        /// </summary>
        /// <returns></returns>
        public NoteObject GenerateGroundDynamicNote(IGroundNoteGenerationData generationData, INoteSpawnData spawnData)
        {
            if (spaceJudgableDataSet == null) { Debug.LogWarning($"SpaceJudgableDataSetがセットされていません"); }

            var note = generator.Value.GenerateNote(generationData, spaceJudgableDataSet);
            spawnData.NoteGameObject = note.gameObject;

            return note;
        }
    }

    /// <summary>
    /// ダイナミックノーツ(Space,Slider判定)のFactoryClass
    /// </summary>
    [System.Serializable]
    public class SpaceDynamicNoteFactory
    {
        [SerializeField] SerializeInterface<IDynamicNoteGenerator> generator;

        SpaceJudgableDataSet spaceJudgableDataSet;

        /// <summary>
        /// ノーツ生成
        /// </summary>
        /// <returns></returns>
        public NoteObject GenerateDynamicNote(ISpaceNoteGenerationData generationData, INoteSpawnData spawnData)
        {
            if (spaceJudgableDataSet == null) { Debug.LogWarning($"SpaceJudgableDataSetがセットされていません"); }

            var note = generator.Value.GenerateNote(generationData, spaceJudgableDataSet);
            spawnData.NoteGameObject = note.gameObject;

            return note;
        }
    }

    /// <summary>
    /// 総括グラウンドタッチノーツ工場
    /// </summary>
    [System.Serializable]
    public class GroundTouchNotesFactory
    {
        [SerializeField] TouchNoteFactory groundTouchNoteFactory;

        public List<NoteObject> GenerateGroundTouchNote(List<GroundTouchNoteData> dataList)
        {
            List<NoteObject> list = new List<NoteObject>();
            foreach (GroundTouchNoteData data in dataList)
            {
                list.Add(groundTouchNoteFactory.GenerateTouchNote(data, data));
            }

            return list;
        }
    }
}