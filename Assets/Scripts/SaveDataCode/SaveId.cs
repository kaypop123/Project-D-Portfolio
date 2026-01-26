using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SaveId : MonoBehaviour
{
    [SerializeField] string guid;
    public string Guid => guid;
#if UNITY_EDITOR

    void OnValidate()
    {
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Awake()
    {
        if (string.IsNullOrEmpty(guid))
            guid = System.Guid.NewGuid().ToString("N");

    }
}