using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingCard : MonoBehaviour
{
    [Header("Card UI Elements")]
    [SerializeField] private Button cardButton;
    [SerializeField] private Image buildingIcon;
    [SerializeField] private Image backgroundImage;

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI buildingSizeText;
    [SerializeField] private TextMeshProUGUI buildingCostText;
    [SerializeField] private TextMeshProUGUI buildingDescriptionText;

    [Header("Visual States")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.gray;
    [SerializeField] private Color selectedColor = Color.yellow;

    private BuildingConfig buildingConfig;
    private System.Action<BuildingConfig> onClickCallback;
    private bool isAffordable = true;
    private bool isSelected = false;

    public void SetupCard(BuildingConfig config, System.Action<BuildingConfig> callback, bool affordable = true)
    {
        buildingConfig = config;
        onClickCallback = callback;
        isAffordable = affordable;

        UpdateCardDisplay();

        // Налаштовуємо кнопку
        if (cardButton != null)
            cardButton.onClick.AddListener(OnCardClicked);
    }

    private void UpdateCardDisplay()
    {
        // Іконка будинку
        if (buildingIcon != null && buildingConfig.icon != null)
        {
            buildingIcon.sprite = buildingConfig.icon;
            buildingIcon.color = isAffordable ? Color.white : unaffordableColor;
        }

        // Назва будинку
        if (buildingNameText != null)
        {
            buildingNameText.text = buildingConfig.buildingName;
            buildingNameText.color = isAffordable ? Color.black : unaffordableColor;
        }

        // Розмір будинку
        if (buildingSizeText != null)
        {
            buildingSizeText.text = $"Розмір: {buildingConfig.size.x}×{buildingConfig.size.y}";
        }

        // Вартість будинку
        if (buildingCostText != null)
        {
            buildingCostText.text = $"${buildingConfig.cost}";
            buildingCostText.color = isAffordable ? Color.green : Color.red;
        }

        // Опис будинку
        if (buildingDescriptionText != null)
        {
            string description = !string.IsNullOrEmpty(buildingConfig.description)
                ? buildingConfig.description
                : "Звичайний будинок для жителів міста.";
            buildingDescriptionText.text = description;
        }

        // Колір фону картки
        if (backgroundImage != null)
        {
            if (isSelected)
                backgroundImage.color = selectedColor;
            else if (isAffordable)
                backgroundImage.color = affordableColor;
            else
                backgroundImage.color = unaffordableColor;
        }

        // Активність кнопки
        if (cardButton != null)
        {
            cardButton.interactable = isAffordable;
        }
    }

    private void OnCardClicked()
    {
        if (isAffordable)
        {
            onClickCallback?.Invoke(buildingConfig);

            // Візуальний ефект кліку
            StartCoroutine(ClickEffect());
        }
    }

    private System.Collections.IEnumerator ClickEffect()
    {
        Vector3 originalScale = transform.localScale;

        // Зменшуємо
        transform.localScale = originalScale * 0.95f;
        yield return new WaitForSeconds(0.1f);

        // Повертаємо
        transform.localScale = originalScale;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateCardDisplay();
    }

    public void SetAffordable(bool affordable)
    {
        isAffordable = affordable;
        UpdateCardDisplay();
    }
}
