using UnityEngine;

public class LocalSingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // シングルトンのインスタンス
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // シーン内からゲームオブジェクトを検索
                instance = FindObjectOfType<T>();

                if (instance == null)
                {
                    // インスタンスが見つからなかった場合、新しいゲームオブジェクトを作成
                    GameObject singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).ToString() + " (Singleton)";
                }
            }
            return instance;
        }
    }

    // Awakeメソッドで重複インスタンスをチェック
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
