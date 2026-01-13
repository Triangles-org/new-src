using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Text.RegularExpressions;

public class EditorMenu : MonoBehaviour
{
    public GameObject buildTab;
    public GameObject sampleObject;
    public int gameObjects;
    private readonly Dictionary<GameObject, int> objectMap = new();
    private GameObject selectedObj = null;
    private int selectedId = -1;
    private Vector2 pos1 = new(-5, -5);
    private Vector2 pos2 = new(2495, 495);
    private readonly HashSet<Vector2> placedPositions = new();
    public new Camera camera;
    public GameObject actionPanel;
    private const int gridSize = 10;
    bool isDragging = false;
    Vector2 pointerDownPos;
    bool pointerDown = false;
    bool dragDuringHold = false;
    bool pointerReleased = false;
    private float zoomLevel = 35f;
    private const float cameraSpeed = 0.2f;
    public GameObject editTab;
    public Button normalUpButton;
    public Button normalDownButton;
    public Button normalLeftButton;
    public Button normalRightButton;
    public Button doubleUpButton;
    public Button doubleDownButton;
    public Button doubleLeftButton;
    public Button doubleRightButton;
    private SpriteRenderer selectedObject;
    public Button deleteButton;
    public Button quickDeleteButton;
    public GameObject pausePanel;
    public Button pauseButton;
    public Button pauseResumeButton;
    public Button saveAndPlayResumeButton;
    public Button saveAndExitResumeButton;
    public Button saveResumeButton;
    public Button exitResumeButton;
    public GameObject objectContainer;
    private float prevPinchDistance = -1;
    public TMP_Text objectsText;
    public Button zoomInButton;
    public Button zoomOutButton;
    private readonly int maxObjects = 4000;
    private string levelUuid;
    private JArray backgroundColor = new(40, 125, 255);
    private JArray groundColor = new(0, 102, 255);
    private int selectedSong = 1;
    public Button editObjectButton;
    public GameObject editObjectPanel;
    public Button editObjectCloseButton;
    public ColorChangeManager editColorPanel;
    private ColorChangeManager editColorPanelClone;
    private int colorEditMode = -1;
    public GameObject levelSettingsPanel;
    public Button levelSettingsButton;
    public Button levelSettingsPanelBackgroundButton;
    public Button levelSettingsPanelGroundButton;
    public Slider levelSettingsPanelSongSlider;
    public TMP_Text levelSettingsPanelSongSliderText;

    async void Start()
    {
        if (BazookaManager.Instance.tempData["targetEditorUuid"] == null)
        {
            await SceneManager.LoadSceneAsync("CreatedLevelsMenu");
            return;
        }
        GenerateGridLines();

        for (int i = 0; i < gameObjects; i++)
        {
            GameObject obj = Instantiate(sampleObject, buildTab.transform);
            obj.name = i.ToString();
            obj.transform.GetChild(0).GetComponent<Image>().sprite = Resources.Load<Sprite>($"Objects/{i + 1}");
            obj.SetActive(true);
            objectMap[obj] = i;

            if (!obj.TryGetComponent<Button>(out var btn)) btn = obj.AddComponent<Button>();
            GameObject current = obj;
            btn.onClick.AddListener(() =>
            {
                if (selectedId == objectMap[current])
                {
                    selectedId = -1;
                    selectedObj = null;
                    UpdateSelectionVisuals();
                }
                else
                {
                    selectedId = objectMap[current];
                    selectedObj = current;
                    UpdateSelectionVisuals();
                }
            });
        }

        normalUpButton.onClick.AddListener(() => MoveSelectedY(1));
        normalDownButton.onClick.AddListener(() => MoveSelectedY(-1));
        normalLeftButton.onClick.AddListener(() => MoveSelectedX(-1));
        normalRightButton.onClick.AddListener(() => MoveSelectedX(1));
        doubleUpButton.onClick.AddListener(() => MoveSelectedY(10));
        doubleDownButton.onClick.AddListener(() => MoveSelectedY(-10));
        doubleLeftButton.onClick.AddListener(() => MoveSelectedX(-10));
        doubleRightButton.onClick.AddListener(() => MoveSelectedX(10));
        deleteButton.onClick.AddListener(DeleteSelectedObject);
        quickDeleteButton.onClick.AddListener(DeleteSelectedObject);
        pauseButton.onClick.AddListener(() =>
        {
            objectsText.text = $"Objects: {GetTotalObjects()}/{maxObjects}";
            pausePanel.SetActive(true);
        });
        pauseResumeButton.onClick.AddListener(() =>
        {
            pausePanel.SetActive(false);
        });
        saveAndPlayResumeButton.onClick.AddListener(async () =>
        {
            if (SaveLevel())
            {
                BazookaManager.Instance.tempData["targetEditorUuid"] = levelUuid;
                await SceneManager.LoadSceneAsync("GamePlayer");
            }
        });
        saveAndExitResumeButton.onClick.AddListener(async () =>
        {
            if (SaveLevel())
            {
                BazookaManager.Instance.tempData["targetEditorUuid"] = levelUuid;
                await SceneManager.LoadSceneAsync("CreatedLevelsMenu");
            }
        });
        saveResumeButton.onClick.AddListener(() =>
        {
            StartCoroutine(SaveButton());
        });
        exitResumeButton.onClick.AddListener(async () =>
        {
            BazookaManager.Instance.tempData["targetEditorUuid"] = levelUuid;
            await SceneManager.LoadSceneAsync("CreatedLevelsMenu");
        });
        zoomInButton.onClick.AddListener(ZoomIn);
        zoomOutButton.onClick.AddListener(ZoomOut);
        editObjectButton.onClick.AddListener(EditButtonPress);
        editObjectCloseButton.onClick.AddListener(EditCloseButtonPress);
        levelSettingsPanelSongSlider.onValueChanged.AddListener(value =>
        {
            levelSettingsPanelSongSliderText.text = value switch
            {
                1 => "1: " + SongKeys.SONG_1,
                2 => "2: " + SongKeys.SONG_2,
                3 => "3: " + SongKeys.SONG_3,
                4 => "4: " + SongKeys.SONG_4,
                5 => "5: " + SongKeys.SONG_5,
                _ => "N/A",
            };
        });
        levelSettingsButton.onClick.AddListener(() =>
        {
            levelSettingsPanelSongSlider.value = selectedSong;
            levelSettingsPanel.SetActive(true);
            editObjectPanel.SetActive(true);
        });
        levelSettingsPanelBackgroundButton.onClick.AddListener(() =>
        {
            var editColorPanelTempClone = Instantiate(editColorPanel.gameObject, editColorPanel.transform.parent);
            editColorPanelTempClone.SetActive(true);
            levelSettingsPanel.SetActive(false);
            editColorPanelClone = editColorPanelTempClone.GetComponent<ColorChangeManager>();
            colorEditMode = 0;
            editColorPanelClone.fadeInputField.transform.parent.gameObject.SetActive(false);
            editColorPanelClone.RSlider.value = int.Parse(backgroundColor[0].ToString());
            editColorPanelClone.GSlider.value = int.Parse(backgroundColor[1].ToString());
            editColorPanelClone.BSlider.value = int.Parse(backgroundColor[2].ToString());
            editColorPanelClone.previewImage.color = new Color(editColorPanelClone.RSlider.value / 255f, editColorPanelClone.GSlider.value / 255f, editColorPanelClone.BSlider.value / 255f);
        });
        levelSettingsPanelGroundButton.onClick.AddListener(() =>
        {
            var editColorPanelTempClone = Instantiate(editColorPanel.gameObject, editColorPanel.transform.parent);
            editColorPanelTempClone.SetActive(true);
            levelSettingsPanel.SetActive(false);
            editColorPanelClone = editColorPanelTempClone.GetComponent<ColorChangeManager>();
            colorEditMode = 0;
            editColorPanelClone.fadeInputField.transform.parent.gameObject.SetActive(false);
            editColorPanelClone.RSlider.value = int.Parse(groundColor[0].ToString());
            editColorPanelClone.GSlider.value = int.Parse(groundColor[1].ToString());
            editColorPanelClone.BSlider.value = int.Parse(groundColor[2].ToString());
            editColorPanelClone.previewImage.color = new Color(editColorPanelClone.RSlider.value / 255f, editColorPanelClone.GSlider.value / 255f, editColorPanelClone.BSlider.value / 255f);
        });

        levelUuid = BazookaManager.Instance.tempData["targetEditorUuid"].ToString();
        BazookaManager.Instance.tempData.Remove("targetEditorUuid");
        JObject data = BazookaManager.Instance.Load($"levels/{levelUuid}.json");
        JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
        List<Level> currentLevelsList = currentLevels.ToObject<List<Level>>();
        foreach (var level in currentLevelsList)
        {
            if (level.UUID == levelUuid)
            {
                if (level.EditorCameraPos != null)
                {
                    camera.transform.position = new Vector3(
                        float.Parse(level.EditorCameraPos[0].ToString()),
                        float.Parse(level.EditorCameraPos[1].ToString()),
                        -10
                    );
                }
                zoomLevel = level.EditorCameraZoom;
            }
        }
        camera.orthographicSize = zoomLevel;

        if (data["songId"] != null)
        {
            selectedSong = int.Parse(data["songId"].ToString());
        }
        if (data["backgroundColor"] != null)
        {
            backgroundColor = JArray.Parse(data["backgroundColor"].ToString());
            Camera.main.backgroundColor = new Color(
                int.Parse(backgroundColor[0].ToString()) / 255f,
                int.Parse(backgroundColor[1].ToString()) / 255f,
                int.Parse(backgroundColor[2].ToString()) / 255f
            );
        }
        if (data["groundColor"] as JArray != null)
        {
            groundColor = JArray.Parse(data["groundColor"].ToString());
        }
        if (data["data"] != null)
        {
            foreach (var objectInfo in JArray.Parse(data["data"].ToString()))
            {
                var sr = PlaceObject(
                    float.Parse(objectInfo["position"][0].ToString()),
                    float.Parse(objectInfo["position"][1].ToString()),
                    float.Parse(objectInfo["position"][2].ToString()),
                    float.Parse(objectInfo["rotation"].ToString()),
                    int.Parse(objectInfo["id"].ToString())
                );
                if (objectInfo["id"].ToString() == "13" || objectInfo["id"].ToString() == "14")
                {
                    if (objectInfo["colorData"] != null)
                    {
                        int r = int.Parse(objectInfo["colorData"][0].ToString());
                        int g = int.Parse(objectInfo["colorData"][1].ToString());
                        int b = int.Parse(objectInfo["colorData"][2].ToString());
                        float duration = float.Parse(objectInfo["colorData"][3].ToString());
                        sr.gameObject.name = $"{objectInfo["id"]}({r},{g},{b},{duration})";
                    }
                }
            }
        }
    }

    void UpdateSelectionVisuals()
    {
        foreach (var pair in objectMap)
        {
            Image objectImg = pair.Key.transform.GetChild(0).GetComponent<Image>();
            Image buttonImg = pair.Key.transform.GetComponent<Image>();
            objectImg.color = pair.Key == selectedObj ? new Color(1f, 1f, 1f, 0.75f) : Color.white;
            buttonImg.color = pair.Key == selectedObj ? new Color(1f, 1f, 1f, 0.75f) : Color.white;
        }
    }

    void UnSetSelectedObject()
    {
        if (selectedObject != null)
        {
            selectedObject.color = Color.white;
        }
        selectedObject = null;
        quickDeleteButton.interactable = false;
        editObjectButton.interactable = false;
    }

    void SetSelectedObject(SpriteRenderer sr)
    {
        if (selectedObject != null)
        {
            selectedObject.color = Color.white;
        }
        selectedObject = sr;
        sr.color = Color.green;
        quickDeleteButton.interactable = true;

        if (selectedObject == null) return;
        var selectedName = selectedObject.name;
        if (selectedName.StartsWith("13") || selectedName.StartsWith("14"))
        {
            editObjectButton.interactable = true;
        }
    }

    void DeleteSelectedObject()
    {
        if (selectedObject == null) return;
        Destroy(selectedObject.gameObject);
        selectedObject = null;
        quickDeleteButton.interactable = false;
    }

    void Update()
    {
        if (pausePanel.activeSelf || editObjectPanel.activeSelf) return;
        HandleCameraMovement();
        HandleZoom();

        if (buildTab.activeSelf && selectedId != -1 && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 inputPos = Vector2.zero;
            bool pressed = false;
            bool held = false;
            bool released = false;

            if (Application.isMobilePlatform && Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                inputPos = touch.position.ReadValue();
                pressed = touch.press.wasPressedThisFrame;
                held = touch.press.isPressed;
                released = touch.press.wasReleasedThisFrame;
            }
            else
            {
                var mouse = Mouse.current;
                inputPos = mouse.position.ReadValue();
                pressed = mouse.leftButton.wasPressedThisFrame;
                held = mouse.leftButton.isPressed;
                released = mouse.leftButton.wasReleasedThisFrame;
            }

            if (pressed)
            {
                pointerDown = true;
                pointerDownPos = inputPos;
                dragDuringHold = false;
            }

            if (pointerDown && !dragDuringHold && held)
            {
                if (Vector2.Distance(pointerDownPos, inputPos) > 100f)
                    dragDuringHold = true;
            }

            if (released)
            {
                pointerReleased = true;
                pointerDown = false;
            }

            if (pointerReleased)
            {
                pointerReleased = false;
                if (!dragDuringHold)
                {
                    Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(inputPos);
                    if (mouseWorldPos.x >= pos1.x && mouseWorldPos.x <= pos2.x && mouseWorldPos.y >= pos1.y && mouseWorldPos.y <= pos2.y && Vector2.Distance(pointerDownPos, inputPos) <= 100f)
                    {
                        float snappedX = Mathf.Round(mouseWorldPos.x / 10f) * 10f;
                        float snappedY = Mathf.Round(mouseWorldPos.y / 10f) * 10f;
                        Vector2 gridPos = new(snappedX, snappedY);
                        if (!placedPositions.Contains(gridPos) && GetTotalObjects() < maxObjects)
                        {
                            var sr = PlaceObject(snappedX, snappedY, 0, 0, selectedId + 1);
                            SetSelectedObject(sr);
                        }
                    }
                }
            }
        }
        if (editTab.activeSelf && Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            float threshold = 5f;

            SpriteRenderer closest = null;
            float closestDist = Mathf.Infinity;

            foreach (var obj in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (obj.transform.parent != objectContainer.transform) continue;
                float dist = Vector2.Distance(mouseWorldPos, obj.transform.position);
                if (dist < threshold && dist < closestDist)
                {
                    closestDist = dist;
                    closest = obj;
                }
            }

            if (closest != null) SetSelectedObject(closest);
        }
        if (selectedObject != null && !Application.isMobilePlatform)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                if (Keyboard.current.leftShiftKey.isPressed)
                {
                    MoveSelectedY(1);
                }
                else
                {
                    MoveSelectedY(10);
                }
            }
            else if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                if (Keyboard.current.leftShiftKey.isPressed)
                {
                    MoveSelectedX(-1);
                }
                else
                {
                    MoveSelectedX(-10);
                }
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                if (Keyboard.current.leftShiftKey.isPressed)
                {
                    MoveSelectedY(-1);
                }
                else
                {
                    MoveSelectedY(-10);
                }
            }
            else if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                if (Keyboard.current.leftCtrlKey.isPressed)
                {
                    var clone = Instantiate(selectedObject.gameObject, objectContainer.transform, false);
                    clone.transform.position = selectedObject.gameObject.transform.position;
                    var sr = clone.GetComponent<SpriteRenderer>();
                    clone.name = selectedObject.gameObject.name;
                    SetSelectedObject(sr);
                }
                else if (Keyboard.current.leftAltKey.isPressed)
                {
                    UnSetSelectedObject();
                }
                else
                {
                    if (Keyboard.current.leftShiftKey.isPressed)
                    {
                        MoveSelectedX(1);
                    }
                    else
                    {
                        MoveSelectedX(10);
                    }
                }
            }
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                var rotation = selectedObject.gameObject.transform.rotation.eulerAngles;
                rotation.z = rotation.z == 0 ? 270 : rotation.z == 270 ? 180 : rotation.z == 180 ? 90 : 0;
                selectedObject.gameObject.transform.rotation = Quaternion.Euler(rotation);
            }
            else if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                var rotation = selectedObject.gameObject.transform.rotation.eulerAngles;
                rotation.z = rotation.z == 0 ? 90 : rotation.z == 90 ? 180 : rotation.z == 180 ? 270 : 0;
                selectedObject.gameObject.transform.rotation = Quaternion.Euler(rotation);
            }
            else if (Keyboard.current.deleteKey.wasPressedThisFrame)
            {
                Destroy(selectedObject.gameObject);
                UnSetSelectedObject();
            }
        }
    }

    SpriteRenderer PlaceObject(float x, float y, float z, float rotation, int placeObject)
    {
        placedPositions.Add(new(x, y));
        GameObject obj = new(placeObject.ToString());
        obj.transform.parent = objectContainer.transform;
        obj.transform.position = new Vector3(x, y, z);
        obj.transform.localEulerAngles = new Vector3(0, 0, rotation);
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>($"Objects/{placeObject}");
        sr.drawMode = SpriteDrawMode.Simple;
        obj.transform.localScale = new Vector3(10f / sr.sprite.bounds.size.x, 10f / sr.sprite.bounds.size.y, 1f);
        return sr;
    }

    void HandleCameraMovement()
    {
        Vector2 inputPos = Vector2.zero;
        bool dragging = false;

        if (Application.isMobilePlatform && Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.press.isPressed)
            {
                inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
                dragging = true;
            }
        }
        else
        {
            if (Mouse.current.leftButton.isPressed)
            {
                inputPos = Mouse.current.position.ReadValue();
                dragging = true;
            }
        }

        if (dragging)
        {
            var rectTransform = actionPanel.GetComponent<RectTransform>();
            Vector2 localPos = rectTransform.InverseTransformPoint(inputPos);
            if (rectTransform.rect.Contains(localPos)) return;

            if (!pointerDown)
            {
                pointerDown = true;
                pointerDownPos = inputPos;
                dragDuringHold = false;
            }

            if (!isDragging)
            {
                if (Vector2.Distance(inputPos, pointerDownPos) < 100) return;
                isDragging = true;
                dragDuringHold = true;
                pointerDownPos = inputPos;
                return;
            }

            Vector2 delta = inputPos - pointerDownPos;
            camera.transform.position -= (Vector3)delta * cameraSpeed;
            pointerDownPos = inputPos;
        }
        else
        {
            if (pointerDown)
            {
                pointerDown = false;
                isDragging = false;
            }
        }
    }

    void HandleZoom()
    {
        if (Application.isMobilePlatform && Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            if (touches.Count >= 2)
            {
                Vector2 pos1 = touches[0].position.ReadValue();
                Vector2 pos2 = touches[1].position.ReadValue();

                if (touches[0].press.isPressed && touches[1].press.isPressed)
                {
                    float currentDist = Vector2.Distance(pos1, pos2);
                    if (prevPinchDistance > 0)
                    {
                        float diff = currentDist - prevPinchDistance;
                        zoomLevel -= diff * 0.1f;
                        zoomLevel = Mathf.Clamp(zoomLevel, 15f, 250f);
                        camera.orthographicSize = zoomLevel;
                    }
                    prevPinchDistance = currentDist;
                    return;
                }
            }
            prevPinchDistance = -1;
        }
        else
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
            {
                if (Keyboard.current.leftCtrlKey.isPressed)
                {
                    zoomLevel -= scroll * 5f;
                    zoomLevel = Mathf.Clamp(zoomLevel, 15f, 250f);
                    camera.orthographicSize = zoomLevel;
                }
                else
                {
                    var pos = camera.transform.position;
                    pos.y += scroll * 5f;
                    camera.transform.position = pos;
                }
            }
        }
    }

    void ZoomIn()
    {
        zoomLevel -= 10f;
        zoomLevel = Mathf.Clamp(zoomLevel, 15f, 250f);
        camera.orthographicSize = zoomLevel;
    }

    void ZoomOut()
    {
        zoomLevel += 10f;
        zoomLevel = Mathf.Clamp(zoomLevel, 15f, 250f);
        camera.orthographicSize = zoomLevel;
    }

    void GenerateGridLines()
    {
        float adjustedGridSize = gridSize;
        for (float x = pos1.x; x <= pos2.x; x += adjustedGridSize)
            DrawGridLine(new Vector3(x, pos1.y, 0), new Vector3(x, pos2.y, 0));
        for (float y = pos1.y; y <= pos2.y; y += adjustedGridSize)
            DrawGridLine(new Vector3(pos1.x, y, 0), new Vector3(pos2.x, y, 0));
    }

    void DrawGridLine(Vector3 start, Vector3 end)
    {
        GameObject line = new("GridLine");
        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.25f;
        lineRenderer.endWidth = 0.25f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.sortingOrder = -1000;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(0f, 0f, 0f, 1f);
        lineRenderer.endColor = new Color(0f, 0f, 0f, 1f);
        line.transform.position = new Vector3(line.transform.position.x, line.transform.position.y, -10f);
    }

    void MoveSelectedX(int amount)
    {
        if (selectedObject == null) return;
        var pos = selectedObject.gameObject.transform.position;
        pos.x += amount;
        selectedObject.gameObject.transform.position = ClampToBounds(pos);
    }

    void MoveSelectedY(int amount)
    {
        if (selectedObject == null) return;
        var pos = selectedObject.gameObject.transform.position;
        pos.y += amount;
        selectedObject.gameObject.transform.position = ClampToBounds(pos);
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, pos1.x, pos2.x);
        pos.y = Mathf.Clamp(pos.y, pos1.y, pos2.y);
        return pos;
    }

    bool SaveLevel()
    {
        List<GameObject> objs = new();
        foreach (Transform child in objectContainer.transform)
        {
            objs.Add(child.gameObject);
        }

        JObject levelData = new();
        List<JObject> levelObjectsData = new();

        foreach (GameObject obj in objs)
        {
            var id = int.Parse(obj.name.Split("(")[0]);
            var pos = obj.transform.position;
            var rot = obj.transform.rotation.eulerAngles;

            JObject tempData = new();
            tempData["id"] = id;
            tempData["position"] = new JArray(pos.x, pos.y, pos.z);
            tempData["rotation"] = rot.z;
            if (id.ToString() == "13" || id.ToString() == "14")
            {
                tempData["colorData"] = new JArray(255, 255, 255, 0.5f);
                var match = System.Text.RegularExpressions.Regex.Match(obj.name, id.ToString() + @"\((\d+),(\d+),(\d+),([0-9.]+)\)");
                if (match.Success)
                {
                    int r = int.Parse(match.Groups[1].Value);
                    int g = int.Parse(match.Groups[2].Value);
                    int b = int.Parse(match.Groups[3].Value);
                    float duration = float.Parse(match.Groups[4].Value);
                    tempData["colorData"] = new JArray(r, g, b, duration);
                }
            }

            levelObjectsData.Add(JObject.FromObject(tempData));
        }

        levelData["version"] = 0;
        levelData["backgroundColor"] = backgroundColor;
        levelData["groundColor"] = groundColor;
        levelData["songMode"] = 0;
        levelData["songId"] = selectedSong;
        levelData["data"] = JArray.FromObject(levelObjectsData);

        BazookaManager.Instance.Save($"levels/{levelUuid}.json", levelData);

        JArray currentLevels = BazookaManager.Instance.GetCreatedLevels();
        List<Level> currentLevelsList = currentLevels.ToObject<List<Level>>();
        foreach (var level in currentLevelsList)
        {
            if (level.UUID == levelUuid)
            {
                level.Verified = false;
                level.EditorCameraPos = new JArray(
                    camera.transform.position.x,
                    camera.transform.position.y
                );
                level.EditorCameraZoom = camera.orthographicSize;
            }
        }
        BazookaManager.Instance.SetCreatedLevels(JArray.FromObject(currentLevelsList));

        return true;
    }

    int GetTotalObjects()
    {
        return objectContainer.transform.childCount;
    }

    void EditButtonPress()
    {
        if (selectedObject == null) return;
        var selectedName = selectedObject.name;
        if (selectedName.StartsWith("13"))
        {
            var colorChangerPanel = Instantiate(editColorPanel.gameObject, editColorPanel.transform.parent).GetComponent<ColorChangeManager>();
            editColorPanelClone = colorChangerPanel;
            colorEditMode = 2;
            colorChangerPanel.gameObject.SetActive(true);
            editObjectPanel.SetActive(true);
            var match = Regex.Match(selectedName, @"13\((\d+),(\d+),(\d+),([0-9.]+)\)");
            if (match.Success)
            {
                int r = int.Parse(match.Groups[1].Value);
                int g = int.Parse(match.Groups[2].Value);
                int b = int.Parse(match.Groups[3].Value);
                float duration = float.Parse(match.Groups[4].Value);
                colorChangerPanel.RInputBox.text = r.ToString();
                colorChangerPanel.GInputBox.text = g.ToString();
                colorChangerPanel.BInputBox.text = b.ToString();
                colorChangerPanel.fadeInputField.text = duration.ToString();
            }
        }
        else if (selectedName.StartsWith("14"))
        {
            var colorChangerPanel = Instantiate(editColorPanel.gameObject, editColorPanel.transform.parent).GetComponent<ColorChangeManager>();
            editColorPanelClone = colorChangerPanel;
            colorEditMode = 3;
            colorChangerPanel.gameObject.SetActive(true);
            editObjectPanel.SetActive(true);
            var match = Regex.Match(selectedName, @"14\((\d+),(\d+),(\d+),([0-9.]+)\)");
            if (match.Success)
            {
                int r = int.Parse(match.Groups[1].Value);
                int g = int.Parse(match.Groups[2].Value);
                int b = int.Parse(match.Groups[3].Value);
                float duration = float.Parse(match.Groups[4].Value);
                colorChangerPanel.RInputBox.text = r.ToString();
                colorChangerPanel.GInputBox.text = g.ToString();
                colorChangerPanel.BInputBox.text = b.ToString();
                colorChangerPanel.fadeInputField.text = duration.ToString();
            }
            editObjectPanel.SetActive(true);
        }
    }

    void EditCloseButtonPress()
    {
        if (levelSettingsPanel.activeSelf)
        {
            editObjectPanel.SetActive(false);
            levelSettingsPanel.SetActive(false);
            selectedSong = int.Parse(levelSettingsPanelSongSlider.value.ToString());
        }
        else if (colorEditMode == 0 || colorEditMode == 1)
        {
            float r = editColorPanelClone.RSlider.value;
            float g = editColorPanelClone.GSlider.value;
            float b = editColorPanelClone.BSlider.value;
            Destroy(editColorPanelClone.gameObject);
            if (colorEditMode == 0)
            {
                backgroundColor = new(r, g, b);
                Camera.main.backgroundColor = new Color(r / 255f, g / 255f, b / 255f);
            }
            else
            {
                groundColor = new(r, g, b);
            }
            colorEditMode = -1;
            levelSettingsPanel.SetActive(true);
        }
        else if (colorEditMode == 2 || colorEditMode == 3)
        {
            if (selectedObject == null) return;
            float r = editColorPanelClone.RSlider.value;
            float g = editColorPanelClone.GSlider.value;
            float b = editColorPanelClone.BSlider.value;
            float duration = float.Parse(editColorPanelClone.fadeInputField.text);
            editObjectPanel.SetActive(false);
            Destroy(editColorPanelClone.gameObject);
            colorEditMode = -1;
            selectedObject.name = $"{selectedObject.name.Split("(")[0]}({r},{g},{b},{duration})";
        }
    }

    IEnumerator SaveButton()
    {
        saveResumeButton.interactable = false;
        yield return new WaitForSeconds(5);
        saveResumeButton.interactable = true;
        yield return null;
    }
}
