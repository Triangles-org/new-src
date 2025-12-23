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
                Load();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnApplicationQuit()
    {
        Save();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Save();
        }
    }

    public void Load()
    {
        string path = Path.Join(Application.persistentDataPath, "savefile.json");
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        else
        {
            try
            {
                var tempSaveFile = JObject.Parse(File.ReadAllText(path));
                if (tempSaveFile != null) saveFile = tempSaveFile;
            }
            catch
            {
                Application.Quit();
            }
        }
        if (saveFile["version"] == null || saveFile["version"].ToString() != "0")
        {
            Application.Quit();
        }
    }

    public void Save()
    {
        string path = Path.Join(Application.persistentDataPath, "savefile.json");
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var encoded = Encoding.UTF8.GetBytes(saveFile.ToString(Newtonsoft.Json.Formatting.Indented));
        fileStream.Write(encoded, 0, encoded.Length);
        fileStream.Flush(true);
    }

    public void ResetSave()
    {
        saveFile = new JObject
        {
            ["version"] = "0"
        };
        Save();
    }
}
