using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public static class ImageLoader
{
    public static async UniTask<Sprite> LoadSpriteFromPathAsync(string path, CancellationToken token)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("‰æ‘œƒtƒ@ƒCƒ‹‚ª‘¶İ‚µ‚Ü‚¹‚ñ: " + path);
            return null;
        }

        byte[] fileData = await File.ReadAllBytesAsync(path, token);

        var texture = new Texture2D(2, 2);
        if (!texture.LoadImage(fileData))
        {
            Debug.LogError("‰æ‘œ‚Ì“Ç‚İ‚İ‚É¸”s‚µ‚Ü‚µ‚½");
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }
}