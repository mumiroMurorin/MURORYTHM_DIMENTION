using System.Threading;
using Cysharp.Threading.Tasks;
using SFB;
using UnityEngine;

public static class ImageFileSelector
{
    public static async UniTask<Sprite> SelectImageSpriteAsync(CancellationToken cancellationToken)
    {
        var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Image File", "", extensions, false);

        if (paths == null || paths.Length == 0 || string.IsNullOrWhiteSpace(paths[0]))
        {
            return null;
        }

        return await ImageLoader.LoadSpriteFromPathAsync(paths[0], cancellationToken);
    }
}
