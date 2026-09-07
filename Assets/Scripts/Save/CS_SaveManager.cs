using System.IO;
using UnityEngine;

/// <summary>
/// セーブデータのファイル入出力(スロット1つ、JSON)
/// </summary>
public class CS_SaveManager
{
    private static CS_SaveManager _instance;
    public static CS_SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CS_SaveManager();
            }
            return _instance;
        }
    }

    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "savedata.json");

    public bool HasSaveData()
    {
        return File.Exists(SavePath);
    }

    public void Save(CS_SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public CS_SaveData Load()
    {
        if (!HasSaveData())
        {
            return null;
        }

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<CS_SaveData>(json);
    }
}
