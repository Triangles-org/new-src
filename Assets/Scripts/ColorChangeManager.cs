using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorChangeManager : MonoBehaviour
{
    public static ColorChangeManager Instance;
    public TMP_InputField fadeInputField;
    public Slider fadeSlider;
    public TMP_InputField RInputBox;
    public TMP_InputField GInputBox;
    public TMP_InputField BInputBox;
    public Slider RSlider;
    public Slider GSlider;
    public Slider BSlider;
    public Image previewImage;
    private bool changedWithCode = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        fadeInputField.onValueChanged.AddListener(newValue =>
        {
            if (changedWithCode) return;
            changedWithCode = true;
            try
            {
                float floatValue = float.Parse(newValue);
                fadeSlider.value = Math.Clamp(floatValue, 0, 10);
            }
            catch
            {
                fadeInputField.text = "0";
                fadeSlider.value = 0;
            }
            changedWithCode = false;
        });
        fadeSlider.onValueChanged.AddListener(newValue =>
        {
            if (changedWithCode) return;
            changedWithCode = true;
            fadeInputField.text = MathF.Round(newValue, 2).ToString();
            changedWithCode = false;
        });
        RInputBox.onValueChanged.AddListener(newValue => UpdateBackgroundColorPreview(0));
        GInputBox.onValueChanged.AddListener(newValue => UpdateBackgroundColorPreview(0));
        BInputBox.onValueChanged.AddListener(newValue => UpdateBackgroundColorPreview(0));
        RSlider.onValueChanged.AddListener(newValue => UpdateBackgroundColorPreview(1));
        RSlider.onValueChanged.AddListener(newValue => UpdateBackgroundColorPreview(1));
        RSlider.onValueChanged.AddListener(newValue => UpdateBackgroundColorPreview(1));
    }

    void UpdateBackgroundColorPreview(int mode)
    {
        if (changedWithCode) return;
        changedWithCode = true;
        int r_value;
        try
        {
            r_value = mode == 0 ? Math.Clamp(int.Parse(RInputBox.text), 0, 255) : (int)RSlider.value;
        }
        catch
        {
            r_value = 0;
        }
        RInputBox.text = r_value.ToString();
        RSlider.value = r_value;
        int g_value;
        try
        {
            g_value = mode == 0 ? Math.Clamp(int.Parse(GInputBox.text), 0, 255) : (int)GSlider.value;
        }
        catch
        {
            g_value = 0;
        }
        RInputBox.text = g_value.ToString();
        GSlider.value = g_value;
        int b_value;
        try
        {
            b_value = mode == 0 ? Math.Clamp(int.Parse(BInputBox.text), 0, 255) : (int)BSlider.value;
        }
        catch
        {
            b_value = 0;
        }
        BInputBox.text = b_value.ToString();
        BSlider.value = b_value;
        previewImage.color = new Color(r_value / 255f, g_value / 255f, b_value / 255f);
        changedWithCode = false;
    }
}
