using UnityEngine;
using UnityEngine.Networking;
using SFB;
using System.Threading;
using System.Threading.Tasks;

public class AudioFileSelector
{
    public async Task<AudioClip> SelectAudioFile(CancellationToken cancellationToken)
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "wav", "mp3") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            return await LoadAudioClip(paths[0], cancellationToken);
        }

        return null;
    }

    private async Task<AudioClip> LoadAudioClip(string path, CancellationToken cancellationToken)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
        {
            var request = www.SendWebRequest();

            while (!request.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    www.Abort();
                    Debug.LogWarning("Audio loading was cancelled.");
                    return null;
                }
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerAudioClip.GetContent(www);
            }
            else
            {
                Debug.LogError("Failed to load audio: " + www.error);
                return null;
            }
        }
    }
}
