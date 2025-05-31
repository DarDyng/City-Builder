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
        // ����������� ������ ��������
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);

        // ��������� ������ �������
        CreateBuildingCards();

        // ������������ ���������
        if (menuTitle != null)
            menuTitle.text = "������� �������";
    }

    private void CreateBuildingCards()
    {
        BuildingConfig[] buildings = gridManager.GetAvailableBuildings();

        // ������� ������� ������
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // ��������� ��� ������
        foreach (BuildingConfig building in buildings)
        {
            CreateBuildingCard(building);
        }

        // ��������� ��������
        if (buildingCountText != null)
            buildingCountText.text = $"��������: {buildings.Length} �������";

        // ������� ��������� �����
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
        // ��� ���� ���� ����� �������� ������
        // ���� �� �� ������� �������
        return true;
    }

    private void OnBuildingSelected(BuildingConfig building)
    {
        selectedBuilding = building;

        Debug.Log($"������� �������: {building.buildingName}");

        // �������� ���� �� UI Manager
        if (uiManager != null)
        {
            uiManager.OnBuildingSelectedFromMenu(building);
        }

        // ��������� ����
        CloseMenu();
    }

    public void OpenMenu()
    {
        buildingMenuPanel.SetActive(true);

        // ��������� ������ (�� ������� ���)
        CreateBuildingCards();

        // ������� ����� (�����������)
        if (buildingMenuPanel.TryGetComponent<Animator>(out Animator animator))
        {
            animator.SetTrigger("Open");
        }
    }

    public void CloseMenu()
    {
        buildingMenuPanel.SetActive(false);

        // ������� �������� (�����������)
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