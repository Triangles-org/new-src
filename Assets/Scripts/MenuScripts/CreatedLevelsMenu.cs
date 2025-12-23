using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreatedLevelsMenu : MonoBehaviour
{
    [SerializeField] private Button backButton;

    [SerializeField] private Button defaultCreateButton;
    [SerializeField] private GameObject defaultScrollContent;
    [SerializeField] private GameObject defaultSampleLevel;

    [SerializeField] private Button levelDeleteButton;
    [SerializeField] private TMP_InputField levelNameInput;
    [SerializeField] private TMP_InputField levelDescriptionInput;
    [SerializeField] private Button levelEditLevelButton;
    [SerializeField] private Button levelPlaytestButton;
    [SerializeField] private Button levelShareButton;

    void Start()
    {
        
    }
}
