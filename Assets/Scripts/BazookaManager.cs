using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BazookaManager : MonoBehaviour
{
    public static BazookaManager Instance;
    private bool firstLoadDone = false;
    public JObject saveFile = new()
    {
        ["version"] = "0"
    };

    public JObject tempData = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (!firstLoadDone)
            {
                firstLoadDone = true;
                saveFile = Load("savefile.json");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnApplicationQuit()
    {
        Save("savefile.json", saveFile);
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Save("savefile.json", saveFile);
        }
    }

    public JObject Load(string pathSuffix)
    {
        string path = Path.Combine(Application.persistentDataPath, pathSuffix);
        string dir = Path.GetDirectoryName(path);

        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "{\"version\":\"0\"}");
            return new()
            {
                ["version"] = "0"
            };
        }

        try
        {
            return JObject.Parse(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return new()
            {
                ["version"] = "0"
            };
        }
    }

    public void Save(string pathSuffix, JObject data)
    {
        string path = Path.Join(Application.persistentDataPath, pathSuffix);
        string dir = Path.GetDirectoryName(path);

        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var encoded = Encoding.UTF8.GetBytes(data.ToString(Newtonsoft.Json.Formatting.None));
        fileStream.Write(encoded, 0, encoded.Length);
        fileStream.Flush(true);
    }

    public void DeleteSave(string pathSuffix)
    {
        string path = Path.Join(Application.persistentDataPath, pathSuffix);
        string dir = Path.GetDirectoryName(path);

        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (File.Exists(path)) File.Delete(path);
    }

    //levels

    public void SetCreatedLevels(JArray value)
    {
        if (saveFile["levels"] == null) saveFile["levels"] = new JObject();
        saveFile["levels"]["createdLevels"] = value;
    }

    public JArray GetCreatedLevels()
    {
        if (saveFile["levels"] == null) return new JArray();
        if (saveFile["levels"]["createdLevels"] == null) return new JArray();
        return JArray.Parse(saveFile["levels"]["createdLevels"].ToString());
    }
}
