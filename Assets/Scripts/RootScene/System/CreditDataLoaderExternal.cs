using System.IO;
using JsonUtil;
using UnityEngine;

public class CreditDataLoaderExternal : MonoBehaviour
{
    [Header("Credit data path")]
    [SerializeField] private string dataFolderName = "CreditData";
    [SerializeField] private string creditFileName = "credit.json";

    [Header("View")]
    [SerializeField] private CreditTextView creditTextView;

    private void Start()
    {
        LoadCreditData();
    }

    private void LoadCreditData()
    {
        if (creditTextView == null)
        {
            Debug.LogWarning("[Credit] CreditTextView is not assigned.");
            return;
        }

        string path = Path.Combine(Application.dataPath, dataFolderName, creditFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning("[Credit] Credit file not found: " + path);
            return;
        }

        if (!JsonLoader.TryLoadFromJsonFile(path, out CreditData creditData))
        {
            Debug.LogWarning("[Credit] Failed to load credit file: " + path);
            return;
        }

        if (creditData == null || creditData.CreditTexts == null || creditData.CreditTexts.Length == 0)
        {
            Debug.LogWarning("[Credit] Credit file has no texts: " + path);
            return;
        }

        creditTextView.SetCreditTexts(creditData.CreditTexts);
    }
}
