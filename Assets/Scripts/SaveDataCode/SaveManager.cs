// SaveManager.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Options")]
    [SerializeField] string folderName = "Saves";   // 저장 폴더
    [SerializeField] string filePrefix = "slot_";   // 파일 접두사
    [SerializeField] bool prettyPrint = true;       // JSON 포맷
    [SerializeField] Camera captureCamera; // 인스펙터로 지정(비워두면 Camera.main)
    Camera Cam => captureCamera != null ? captureCamera : Camera.main;
    [SerializeField] int thumbnailWidth = 512;
    [SerializeField] int thumbnailHeight = 288;
    // Save() 내부: CaptureScreenshotImmediate(slot, thumbnailWidth, thumbnailHeight);

    string ScreenshotPath(int slot) => Path.Combine(SaveDir, $"{filePrefix}{slot}.png");
    string SaveDir => Path.Combine(Application.persistentDataPath, folderName);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);
    }

    string SlotPath(int slot) => Path.Combine(SaveDir, $"{filePrefix}{slot}.json");

    // === 외부 API ===
    public void Save(int slot)
    {
        var save = new SaveFile
        {
            version = 1,
            sceneName = SceneManager.GetActiveScene().name,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            records = new List<SaveRecord>()
        };

        // 씬의 모든 ISaveable 수집
        var saveables = GetAllSaveables();

        foreach (var s in saveables)
        {
            string id = s.GetSaveId();
            if (string.IsNullOrEmpty(id)) continue;

            var stateObj = s.CaptureState();
            if (stateObj == null) continue;

            string typeName = stateObj.GetType().AssemblyQualifiedName;
            string payload = JsonUtility.ToJson(stateObj, prettyPrint);

            save.records.Add(new SaveRecord
            {
                id = id,
                type = typeName,
                payload = payload
            });
        }

        // (옵션) 암호화/압축 훅
        // var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(save, prettyPrint));
        // bytes = MyEncrypt(bytes); bytes = MyCompress(bytes); File.WriteAllBytes(SlotPath(slot), bytes);

        string json = JsonUtility.ToJson(save, prettyPrint);
        File.WriteAllText(SlotPath(slot), json, Encoding.UTF8);

        CaptureScreenshotImmediate(slot, thumbnailWidth, thumbnailHeight);

#if UNITY_EDITOR
        Debug.Log($"[SaveManager] Saved {save.records.Count} records → {SlotPath(slot)}");
#endif
    }

    public bool Load(int slot, bool loadSceneIfDifferent = false)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) { Debug.LogWarning("[SaveManager] No save file"); return false; }

        // (옵션) 복호화/해제 훅
        // var bytes = File.ReadAllBytes(path); bytes = MyDecompress(bytes); bytes = MyDecrypt(bytes);
        // var json = Encoding.UTF8.GetString(bytes);

        string json = File.ReadAllText(path, Encoding.UTF8);
        var save = JsonUtility.FromJson<SaveFile>(json);
        if (save == null) { Debug.LogError("[SaveManager] Corrupt save"); return false; }

        if (loadSceneIfDifferent && save.sceneName != SceneManager.GetActiveScene().name)
        {
            StartCoroutine(LoadSceneThenRestore(save));
            return true;
        }

        RestoreNow(save);
        return true;
    }

    System.Collections.IEnumerator LoadSceneThenRestore(SaveFile save)
    {
        var op = SceneManager.LoadSceneAsync(save.sceneName);
        while (!op.isDone) yield return null;
        yield return null; // 한 프레임 대기
        RestoreNow(save);
    }

    void RestoreNow(SaveFile save)
    {
        // 현재 씬의 ISaveable들을 id->리스트로 매핑
        var map = new Dictionary<string, List<ISaveable>>();
        foreach (var s in GetAllSaveables())
        {
            string id = s.GetSaveId();
            if (string.IsNullOrEmpty(id)) continue;
            if (!map.TryGetValue(id, out var list)) map[id] = list = new List<ISaveable>();
            list.Add(s);
        }

        int applied = 0;
        foreach (var rec in save.records)
        {
            if (!map.TryGetValue(rec.id, out var receivers)) continue;

            var type = Type.GetType(rec.type);
            if (type == null) continue;
            var obj = JsonUtility.FromJson(rec.payload, type);

            foreach (var r in receivers) { r.RestoreState(obj); applied++; }
        }

#if UNITY_EDITOR
        Debug.Log($"[SaveManager] Restored {applied} states (of {save.records.Count})");
#endif
    }
    void CaptureScreenshotImmediate(int slot, int width = 512, int height = 288)
    {
       
        // 변경
        var cam = Cam;
        if (!cam) return;

        var rt = new RenderTexture(width, height, 24);
        var prevRT = RenderTexture.active;
        var prevCamRT = cam.targetTexture;

        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(ScreenshotPath(slot), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }
        finally
        {
            cam.targetTexture = prevCamRT;
            RenderTexture.active = prevRT;
            rt.Release();
            UnityEngine.Object.Destroy(rt);
        }
    }


    // 슬롯 유틸
    public bool HasSlot(int slot) => File.Exists(SlotPath(slot));
    public void DeleteSlot(int slot)
    {
        var p = SlotPath(slot);
        if (File.Exists(p)) File.Delete(p);

        var png = ScreenshotPath(slot);           
        if (File.Exists(png)) File.Delete(png);
    }

    // === 유틸: 모든 ISaveable 수집 (버전별 API 대응) ===
    static List<ISaveable> GetAllSaveables()
    {
#if UNITY_2022_2_OR_NEWER
        // 새로운 API: 정렬 안함, 비활성 포함
        return UnityEngine.Object
            .FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<ISaveable>()
            .ToList();
#else
        // 구버전 호환
        return UnityEngine.Object
            .FindObjectsOfType<MonoBehaviour>(true)
            .OfType<ISaveable>()
            .ToList();
#endif
    }
}
