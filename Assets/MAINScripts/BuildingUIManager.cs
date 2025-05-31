// ===== BuildingUIManager.cs - Адаптований під ваші файли =====
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

        Debug.Log($"GridManager знайдено: {gridManager != null}");
        Debug.Log($"BuildingMenuManager знайдено: {buildingMenuManager != null}");

        if (gridManager != null)
        {
            SetupUI();
            gridManager.OnModeChanged += UpdateModeDisplay;
        }
    }

    private void SetupUI()
    {
        // Налаштовуємо основні кнопки
        if (roadButton != null)
            roadButton.onClick.AddListener(() => ToggleMode(BuildMode.Road));

        if (buildingButton != null)
            buildingButton.onClick.AddListener(() => OpenBuildingMenu());

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => ToggleMode(BuildMode.Delete));

        // Встановлюємо початковий режим
        UpdateModeDisplay(BuildMode.None);

        Debug.Log("BuildingUIManager налаштовано успішно!");
    }

    private void OpenBuildingMenu()
    {
        if (buildingMenuManager != null)
        {
            // Відкриваємо меню ОДРАЗУ, без перевірки режиму
            buildingMenuManager.OpenMenu();

            // Скасовуємо поточний режим, щоб користувач міг вибрати новий будинок
            gridManager.SetBuildMode(BuildMode.None, null);
        }
        else
        {
            Debug.LogWarning("BuildingMenuManager не знайдено! Використовується простий режим.");
            ToggleBuildingMode();
        }
    }

    // Метод для сумісності з BuildingMenuManager
    public void OnBuildingSelectedFromMenu(BuildingConfig building)
    {
        selectedBuilding = building;
        gridManager.SetBuildMode(BuildMode.Building, building);
        Debug.Log($"Встановлено режим будівництва: {building.buildingName}");
    }

    // Fallback метод якщо немає меню
    private void ToggleBuildingMode()
    {
        BuildingConfig[] buildings = gridManager.GetAvailableBuildings();

        if (buildings == null || buildings.Length == 0)
        {
            Debug.LogWarning("Немає доступних будинків!");
            return;
        }

        if (gridManager.CurrentMode == BuildMode.Building)
        {
            // Якщо вже в режимі будівництва - переключаємо на наступний будинок
            int currentIndex = System.Array.IndexOf(buildings, selectedBuilding);
            int nextIndex = (currentIndex + 1) % buildings.Length;
            selectedBuilding = buildings[nextIndex];
            gridManager.SetBuildMode(BuildMode.Building, selectedBuilding);
            Debug.Log($"Переключено на: {selectedBuilding.buildingName}");
        }
        else
        {
            // Встановлюємо режим будівництва з першим будинком
            selectedBuilding = buildings[0];
            gridManager.SetBuildMode(BuildMode.Building, selectedBuilding);
            Debug.Log($"Режим будівництва: {selectedBuilding.buildingName}");
        }
    }

    private void ToggleMode(BuildMode mode)
    {
        if (gridManager.CurrentMode == mode)
        {
            // Якщо вже в цьому режимі, вимикаємо
            gridManager.SetBuildMode(BuildMode.None, null);
        }
        else
        {
            // Встановлюємо новий режим
            gridManager.SetBuildMode(mode, null);
        }
    }

    private void UpdateModeDisplay(BuildMode mode)
    {
        // Оновлюємо текст режиму
        if (modeText != null)
        {
            switch (mode)
            {
                case BuildMode.None:
                    modeText.text = "Режим: Огляд";
                    break;
                case BuildMode.Road:
                    modeText.text = "Режим: Будівництво доріг";
                    break;
                case BuildMode.Building:
                    string buildingName = gridManager.SelectedBuilding != null ?
                        gridManager.SelectedBuilding.buildingName : "Будівництво";
                    modeText.text = $"Режим: {buildingName}";
                    break;
                case BuildMode.Delete:
                    modeText.text = "Режим: Видалення";
                    break;
            }
        }

        // Оновлюємо індикатор кольору
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

    // Публічні методи для зовнішнього використання
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

    // Методи для сумісності з різними системами
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