using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreatedLevelsMenu : MonoBehaviour
{
    [SerializeField] private Button backButton;

    [SerializeField] private GameObject defaultLayer;
    [SerializeField] private Button defaultCreateButton;
    [SerializeField] private GameObject defaultScrollContent;
    [SerializeField] private GameObject defaultSampleLevel;

    [SerializeField] private GameObject levelLayer;
    [SerializeField] private Button levelDeleteButton;
    [SerializeField] private TMP_InputField levelNameInput;
    [SerializeField] private TMP_InputField levelDescriptionInput;
    [SerializeField] private Button levelEditLevelButton;
    [SerializeField] private Button levelPlaytestButton;
    [SerializeField] private Button levelShareButton;
    private string levelUuid;

    void Start()
    {
        ResetLevelView();
        levelDescriptionInput.textComponent.textWrappingMode = TextWrappingModes.Normal;
        backButton.onClick.AddListener(async () =>
        {
            if (defaultLayer.activeSelf)
            {
                await SceneManager.LoadSceneAsync("CreatorLayer");
            }
            else
            {
                LeaveLevel();
            }
        });
        defaultCreateButton.onClick.AddListener(() =>
        {
            JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
            Level newLevel = new(Guid.NewGuid().ToString(), $"Unnamed {currentLevels.Count + 1}", "", false, false, -1, new JArray(0, 0), 35f);
            currentLevels.Add(JObject.FromObject(newLevel));
            BazookaManager.Instance.SetCreatedLevels(currentLevels);
            LoadLevel(newLevel.UUID, currentLevels);
        });
        levelNameInput.onValueChanged.AddListener(newValue => TitleChangeEvent(newValue));
        levelDescriptionInput.onValueChanged.AddListener(newValue => DescriptionChangeEvent(newValue));
        levelEditLevelButton.onClick.AddListener(LaunchEditor);
        levelDeleteButton.onClick.AddListener(() =>
        {
            JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
            var item = currentLevels.FirstOrDefault(x => x["uuid"]?.ToString() == levelUuid);
            item?.Remove();
            BazookaManager.Instance.SetCreatedLevels(currentLevels);
            BazookaManager.Instance.DeleteSave($"levels/{levelUuid}.json");
            LeaveLevel();
        });
        // if (LevelDataManager.Instance.targetSelectionUuid != null)
        // {
        //     LoadLevel(LevelDataManager.Instance.targetSelectionUuid);
        //     LevelDataManager.Instance.targetSelectionUuid = null;
        // }
    }

    void LoadLevel(string targetUuid, JArray currentLevels)
    {
        List<Level> currentLevelsList = currentLevels.ToObject<List<Level>>();
        foreach (var level in currentLevelsList)
        {
            if (level.UUID == targetUuid)
            {
                levelUuid = targetUuid;
                if (level.Name != null)
                {
                    levelNameInput.text = level.Name.Trim()[..Math.Min(16, level.Name.Trim().Length)];
                }
                if (level.Description != null)
                {
                    levelDescriptionInput.text = level.Description.Trim()[..Math.Min(192, level.Description.Trim().Length)];
                }
                levelNameInput.interactable = !level.Uploaded;
                defaultLayer.SetActive(false);
                levelLayer.SetActive(true);
            }
        }
    }

    void LoadLevel(string targetUuid)
    {
        JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
        LoadLevel(targetUuid, currentLevels);
    }

    void LeaveLevel()
    {
        levelUuid = null;
        ResetLevelView();
        defaultCreateButton.transform.localScale = UnityEngine.Vector3.one;
        defaultLayer.SetActive(true);
        levelLayer.SetActive(false);
        levelNameInput.text = "";
        levelDescriptionInput.text = "";
        levelNameInput.interactable = true;
    }

    void LaunchEditor()
    {
        BazookaManager.Instance.tempData["targetEditorUuid"] = levelUuid;
        SceneManager.LoadScene("EditorMenu");
    }

    void TitleChangeEvent(string newValue)
    {
        JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
        List<Level> currentLevelsList = currentLevels.ToObject<List<Level>>();
        foreach (var level in currentLevelsList)
        {
            if (level.UUID == levelUuid)
            {
                if (newValue.Trim() != "")
                {
                    level.Name = newValue.Trim()[..Math.Min(16, newValue.Trim().Length)];
                }
            }
        }
        BazookaManager.Instance.SetCreatedLevels(JArray.FromObject(currentLevelsList));
    }

    void DescriptionChangeEvent(string newValue)
    {
        JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
        List<Level> currentLevelsList = currentLevels.ToObject<List<Level>>();
        foreach (var level in currentLevelsList)
        {
            if (level.UUID == levelUuid)
            {
                level.Description = newValue.Trim()[..Math.Min(192, newValue.Trim().Length)];
            }
        }
        BazookaManager.Instance.SetCreatedLevels(JArray.FromObject(currentLevelsList));
    }

    void ResetLevelView()
    {
        foreach (Transform child in defaultScrollContent.transform)
        {
            GameObject gameObject = child.gameObject;
            if (gameObject.activeSelf)
            {
                Destroy(gameObject);
            }
        }
        defaultScrollContent.transform.localPosition = new(defaultScrollContent.transform.localPosition.x, 0, defaultScrollContent.transform.localPosition.z);
        JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
        List<Level> currentLevelsList = currentLevels.ToObject<List<Level>>();
        foreach (var level in currentLevelsList)
        {
            var newObj = Instantiate(defaultSampleLevel, defaultSampleLevel.transform.parent, false);
            TMP_Text child0 = newObj.transform.GetChild(0).GetComponent<TMP_Text>();
            child0.text = level.Name.Trim()[..Math.Min(16, level.Name.Trim().Length)];
            newObj.transform.GetChild(1).GetComponent<TMP_Text>().text = level.Verified ? "Verified" : "Unverified";
            newObj.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => LoadLevel(level.UUID));
            newObj.SetActive(true);
        }
    }
}
