using UnityEngine;

public class LocalSingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    // �V���O���g���̃C���X�^���X
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // �V�[��������Q�[���I�u�W�F�N�g������
                instance = FindObjectOfType<T>();

                if (instance == null)
                {
                    // �C���X�^���X��������Ȃ������ꍇ�A�V�����Q�[���I�u�W�F�N�g���쐬
                    GameObject singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).ToString() + " (Singleton)";
                }
            }
            return instance;
        }
    }

    // Awake���\�b�h�ŏd���C���X�^���X���`�F�b�N
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
