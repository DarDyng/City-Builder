using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingMenuManager : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] private GameObject buildingMenuPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject buildingCardPrefab;
    [SerializeField] private Button closeButton;

    [Header("Menu Header")]
    [SerializeField] private TextMeshProUGUI menuTitle;
    [SerializeField] private TextMeshProUGUI buildingCountText;

    private GridManager gridManager;
    private BuildingUIManager uiManager;
    private BuildingConfig selectedBuilding;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        uiManager = FindFirstObjectByType<BuildingUIManager>();

        SetupMenu();
        buildingMenuPanel.SetActive(false);
    }

    private void SetupMenu()
    {
        // Налаштовуємо кнопку закриття
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);

        // Створюємо картки будинків
        CreateBuildingCards();

        // Встановлюємо заголовок
        if (menuTitle != null)
            menuTitle.text = "Вибрати будинок";
    }

    private void CreateBuildingCards()
    {
        BuildingConfig[] buildings = gridManager.GetAvailableBuildings();

        // Очищуємо існуючі картки
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Створюємо нові картки
        foreach (BuildingConfig building in buildings)
        {
            CreateBuildingCard(building);
        }

        // Оновлюємо лічильник
        if (buildingCountText != null)
            buildingCountText.text = $"Доступно: {buildings.Length} будинків";

        // Скидаємо прокрутку вгору
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CreateBuildingCard(BuildingConfig building)
    {
        GameObject cardObj = Instantiate(buildingCardPrefab, contentParent);
        BuildingCard card = cardObj.GetComponent<BuildingCard>();

        if (card != null)
        {
            card.SetupCard(building, OnBuildingSelected, IsAffordable(building));
        }
    }

    private bool IsAffordable(BuildingConfig building)
    {
        // Тут може бути логіка перевірки грошей
        // Поки що всі будинки доступні
        return true;
    }

    private void OnBuildingSelected(BuildingConfig building)
    {
        selectedBuilding = building;

        Debug.Log($"Вибрано будинок: {building.buildingName}");

        // Передаємо вибір до UI Manager
        if (uiManager != null)
        {
            uiManager.OnBuildingSelectedFromMenu(building);
        }

        // Закриваємо меню
        CloseMenu();
    }

    public void OpenMenu()
    {
        buildingMenuPanel.SetActive(true);

        // Оновлюємо картки (на випадок змін)
        CreateBuildingCards();

        // Анімація появи (опціонально)
        if (buildingMenuPanel.TryGetComponent<Animator>(out Animator animator))
        {
            animator.SetTrigger("Open");
        }
    }

    public void CloseMenu()
    {
        buildingMenuPanel.SetActive(false);

        // Анімація закриття (опціонально)
        if (buildingMenuPanel.TryGetComponent<Animator>(out Animator animator))
        {
            animator.SetTrigger("Close");
        }
    }

    public bool IsMenuOpen()
    {
        return buildingMenuPanel.activeInHierarchy;
    }

    public void RefreshMenu()
    {
        if (IsMenuOpen())
        {
            CreateBuildingCards();
        }
    }
}