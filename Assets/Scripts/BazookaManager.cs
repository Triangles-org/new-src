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

    public JObject Load(String pathSuffix)
    {
        string path = Path.Join(Application.persistentDataPath, pathSuffix);
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        else
        {
            try
            {
                var tempSaveFile = JObject.Parse(File.ReadAllText(path));
                return tempSaveFile;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public void Save(String pathSuffix, JObject data)
    {
        string path = Path.Join(Application.persistentDataPath, pathSuffix);
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var encoded = Encoding.UTF8.GetBytes(data.ToString(Newtonsoft.Json.Formatting.Indented));
        fileStream.Write(encoded, 0, encoded.Length);
        fileStream.Flush(true);
    }

    public void ResetSave()
    {
        saveFile = new JObject
        {
            ["version"] = "0"
        };
        Save("savefile.json", saveFile);
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
