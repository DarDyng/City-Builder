// ===== BuildingUIManager.cs - ����������� �� ���� ����� =====
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingUIManager : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField] private Button roadButton;
    [SerializeField] private Button buildingButton;
    [SerializeField] private Button deleteButton;

    [Header("Mode Display")]
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private Image modeIndicator;
    [SerializeField] private Color roadModeColor = Color.yellow;
    [SerializeField] private Color buildingModeColor = Color.green;
    [SerializeField] private Color deleteModeColor = Color.red;
    [SerializeField] private Color normalModeColor = Color.white;

    private GridManager gridManager;
    private BuildingMenuManager buildingMenuManager;
    private BuildingConfig selectedBuilding;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        buildingMenuManager = FindFirstObjectByType<BuildingMenuManager>();

        Debug.Log($"GridManager ��������: {gridManager != null}");
        Debug.Log($"BuildingMenuManager ��������: {buildingMenuManager != null}");

        if (gridManager != null)
        {
            SetupUI();
            gridManager.OnModeChanged += UpdateModeDisplay;
        }
    }

    private void SetupUI()
    {
        // ����������� ������ ������
        if (roadButton != null)
            roadButton.onClick.AddListener(() => ToggleMode(BuildMode.Road));

        if (buildingButton != null)
            buildingButton.onClick.AddListener(() => OpenBuildingMenu());

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => ToggleMode(BuildMode.Delete));

        // ������������ ���������� �����
        UpdateModeDisplay(BuildMode.None);

        Debug.Log("BuildingUIManager ����������� ������!");
    }

    private void OpenBuildingMenu()
    {
        if (buildingMenuManager != null)
        {
            // ³�������� ���� ������, ��� �������� ������
            buildingMenuManager.OpenMenu();

            // ��������� �������� �����, ��� ���������� �� ������� ����� �������
            gridManager.SetBuildMode(BuildMode.None, null);
        }
        else
        {
            Debug.LogWarning("BuildingMenuManager �� ��������! ��������������� ������� �����.");
            ToggleBuildingMode();
        }
    }

    // ����� ��� �������� � BuildingMenuManager
    public void OnBuildingSelectedFromMenu(BuildingConfig building)
    {
        selectedBuilding = building;
        gridManager.SetBuildMode(BuildMode.Building, building);
        Debug.Log($"����������� ����� ����������: {building.buildingName}");
    }

    // Fallback ����� ���� ���� ����
    private void ToggleBuildingMode()
    {
        BuildingConfig[] buildings = gridManager.GetAvailableBuildings();

        if (buildings == null || buildings.Length == 0)
        {
            Debug.LogWarning("���� ��������� �������!");
            return;
        }

        if (gridManager.CurrentMode == BuildMode.Building)
        {
            // ���� ��� � ����� ���������� - ����������� �� ��������� �������
            int currentIndex = System.Array.IndexOf(buildings, selectedBuilding);
            int nextIndex = (currentIndex + 1) % buildings.Length;
            selectedBuilding = buildings[nextIndex];
            gridManager.SetBuildMode(BuildMode.Building, selectedBuilding);
            Debug.Log($"����������� ��: {selectedBuilding.buildingName}");
        }
        else
        {
            // ������������ ����� ���������� � ������ ��������
            selectedBuilding = buildings[0];
            gridManager.SetBuildMode(BuildMode.Building, selectedBuilding);
            Debug.Log($"����� ����������: {selectedBuilding.buildingName}");
        }
    }

    private void ToggleMode(BuildMode mode)
    {
        if (gridManager.CurrentMode == mode)
        {
            // ���� ��� � ����� �����, ��������
            gridManager.SetBuildMode(BuildMode.None, null);
        }
        else
        {
            // ������������ ����� �����
            gridManager.SetBuildMode(mode, null);
        }
    }

    private void UpdateModeDisplay(BuildMode mode)
    {
        // ��������� ����� ������
        if (modeText != null)
        {
            switch (mode)
            {
                case BuildMode.None:
                    modeText.text = "�����: �����";
                    break;
                case BuildMode.Road:
                    modeText.text = "�����: ���������� ����";
                    break;
                case BuildMode.Building:
                    string buildingName = gridManager.SelectedBuilding != null ?
                        gridManager.SelectedBuilding.buildingName : "����������";
                    modeText.text = $"�����: {buildingName}";
                    break;
                case BuildMode.Delete:
                    modeText.text = "�����: ���������";
                    break;
            }
        }

        // ��������� ��������� �������
        if (modeIndicator != null)
        {
            switch (mode)
            {
                case BuildMode.None:
                    modeIndicator.color = normalModeColor;
                    break;
                case BuildMode.Road:
                    modeIndicator.color = roadModeColor;
                    break;
                case BuildMode.Building:
                    modeIndicator.color = buildingModeColor;
                    break;
                case BuildMode.Delete:
                    modeIndicator.color = deleteModeColor;
                    break;
            }
        }
    }

    // ������ ������ ��� ���������� ������������
    public void CancelCurrentMode()
    {
        selectedBuilding = null;
        gridManager.SetBuildMode(BuildMode.None, null);
    }

    public void SetRoadMode()
    {
        gridManager.SetBuildMode(BuildMode.Road, null);
    }

    public void SetDeleteMode()
    {
        gridManager.SetBuildMode(BuildMode.Delete, null);
    }

    public void NextBuilding()
    {
        ToggleBuildingMode();
    }

    // ������ ��� �������� � ������ ���������
    public void OnBuildingSelected(BuildingConfig building)
    {
        OnBuildingSelectedFromMenu(building);
    }

    void OnDestroy()
    {
        if (gridManager != null)
        {
            gridManager.OnModeChanged -= UpdateModeDisplay;
        }
    }
}