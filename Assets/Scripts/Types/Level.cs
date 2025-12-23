using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Level
{
    [JsonProperty("uuid")]
    public string UUID { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("verified")]
    public bool Verified { get; set; }

    [JsonProperty("uploaded")]
    public bool Uploaded { get; set; }

    [JsonProperty("uploadedId")]
    public BigInteger UploadedID { get; set; }

    [JsonProperty("editorCameraPos")]
    public JArray EditorCameraPos { get; set; }

    [JsonProperty("editorCameraZoom")]
    public float EditorCameraZoom { get; set; }

    public Level(string uuid, string name, string description, bool verified, bool uploaded, BigInteger uploadedId, JArray editorCameraPos, float editorCameraZoom)
    {
        UUID = uuid;
        Name = name;
        Description = description;
        Verified = verified;
        Uploaded = uploaded;
        UploadedID = uploadedId;
        EditorCameraPos = editorCameraPos;
        EditorCameraZoom = editorCameraZoom;
    }
}