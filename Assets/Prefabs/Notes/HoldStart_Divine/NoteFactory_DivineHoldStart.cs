using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteFactory_DivineHoldStart : NoteFactory<NoteData_DivineHoldStart>
{
    [SerializeField] GameObject noteObjectOriginPrefab;
    [SerializeField] NoteJudgementSettings judgementSettings;

    [Header("Tiles")]
    [SerializeField] GameObject singleTilePrefab;
    [SerializeField] GameObject rightEdgeTilePrefab;
    [SerializeField] GameObject centerTilePrefab;
    [SerializeField] GameObject leftEdgeTilePrefab;

    INoteSpawnDataOptionGetter optionHolder;
    ISliderInputGetter sliderInputGetter;
    IJudgementRecorder judgementRecorder;
    ITimeGetter timer;
    Transform noteParent;

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        optionHolder = initializingData.OptionHolder;
        noteParent = initializingData.NoteParent;
        sliderInputGetter = initializingData.SliderInputGetter;
        judgementRecorder = initializingData.JudgementRecorder;
        timer = initializingData.Timer;
    }

    public override NoteObject<NoteData_DivineHoldStart> Spawn(NoteData_DivineHoldStart data, INotePositionCalculator positionCalculator)
    {
        NoteObject<NoteData_DivineHoldStart> note = GenerateNoteInstance(ConvertNoteData(data));
        SetTransform(note, positionCalculator.GetPosition(data.Timing) * optionHolder.NoteSpeed.Value);
        note.Initialize(data);
        return note;
    }

    private NoteData_DivineHoldStart ConvertNoteData(NoteData_DivineHoldStart data)
    {
        data.SliderInput = sliderInputGetter;
        data.Timer = timer;
        data.JudgementRecorder = judgementRecorder;
        data.OptionGetter = optionHolder;
        data.JudgementSettings = judgementSettings;
        if (judgementSettings != null)
        {
            data.JudgementWindow = judgementSettings.CreateJudgementWindowIfMissing(data.JudgementWindow);
        }
        return data;
    }

    private NoteObject<NoteData_DivineHoldStart> GenerateNoteInstance(NoteData_DivineHoldStart data)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);
        GameObject noteObj = GenerateNoteObject(data.Range.Length);
        noteObj.transform.SetParent(origin.transform);
        noteObj.transform.eulerAngles = new Vector3(0, 0, CalcNoteTransform.NoteAngle(data.Range));
        return origin.GetComponent<NoteObject<NoteData_DivineHoldStart>>();
    }

    private GameObject GenerateNoteObject(int size)
    {
        Vector3 pos, rot;
        GameObject pre = new GameObject("NoteObjects");
        NoteLayerUtility.SetNotesLayer(pre);

        for (int i = 0; i < size; i++)
        {
            float radian = (5.625f * (2 * i - size + 1) - 90f) * Mathf.Deg2Rad;
            pos = new Vector3(10 * Mathf.Cos(radian), 10 * Mathf.Sin(radian), 0);
            rot = new Vector3(0, 0, ((size - 1) / 2f - (size - i - 1)) * 11.25f);

            if (size == 1) { Instantiate(singleTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            else if (i == 0) { Instantiate(leftEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            else if (i == size - 1) { Instantiate(rightEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            else { Instantiate(centerTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
        }

        return pre;
    }

    private void SetTransform(NoteObject<NoteData_DivineHoldStart> note, float spawnZ)
    {
        note.transform.SetParent(noteParent);
        note.SetPosition(spawnZ, optionHolder.NoteCurveRadius.Value);
    }
}
