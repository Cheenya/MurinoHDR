using System;
using System.IO;
using UnityEngine;

namespace MurinoHDR.Save;

[Serializable]
public sealed class SaveData
{
    public int Seed;
}

public sealed class SaveSystem : MonoBehaviour
{
    private const string FileName = "save.json";

    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public void Save(SaveData data)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SAVE] Saved to {SavePath}");
    }

    public SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SAVE] No save file, using defaults");
            return new SaveData();
        }

        var json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        Debug.Log($"[SAVE] Loaded from {SavePath}");
        return data;
    }
}
