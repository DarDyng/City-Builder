using UnityEngine;
using System.Collections.Generic;

public class TerrainDecorator : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private int borderSize = 20; // Розмір бордюру навколо основної зони
    [SerializeField] private float tileSize = 20f; // Розмір однієї клітинки

    [Header("Prefabs")]
    [SerializeField] private GameObject grassTilePrefab; // Префаб трави
    [SerializeField] private GameObject[] plantPrefabs; // Масив префабів рослин

    [Header("Plant Settings")]
    [SerializeField][Range(0f, 1f)] private float plantDensity = 0.8f; // Ймовірність появи рослини
    [SerializeField] private float minScale = 0.8f; // Мінімальний масштаб рослини
    [SerializeField] private float maxScale = 1.2f; // Максимальний масштаб рослини
    [SerializeField] private float maxRotationY = 360f; // Максимальний поворот

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private Transform grassParent;
    private Transform plantsParent;
    private int gridSize = 20; // Розмір основної сітки

    void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
            gridSize = gridManager.GridWidth;

        GenerateTerrainDecoration();
    }

    public void GenerateTerrainDecoration()
    {
        Debug.Log("Починаємо генерацію природного оточення...");

        // Створюємо батьківські об'єкти
        CreateParentObjects();

        // Генеруємо траву
        GenerateGrassBorder();

        // Розміщуємо рослини
        PlacePlants();

        Debug.Log("Генерація природного оточення завершена!");
    }

    private void CreateParentObjects()
    {
        // Видаляємо старі об'єкти якщо є
        if (grassParent != null)
            DestroyImmediate(grassParent.gameObject);
        if (plantsParent != null)
            DestroyImmediate(plantsParent.gameObject);

        // Створюємо нові
        grassParent = new GameObject("TerrainGrass").transform;
        plantsParent = new GameObject("TerrainPlants").transform;

        grassParent.parent = transform;
        plantsParent.parent = transform;
    }

    private void GenerateGrassBorder()
    {
        if (grassTilePrefab == null)
        {
            Debug.LogError("Grass Tile Prefab не призначено!");
            return;
        }

        // Границі розширеної зони
        int minBound = -borderSize;
        int maxBound = gridSize + borderSize;

        int grassCount = 0;

        // Генеруємо траву навколо основної зони
        for (int x = minBound; x < maxBound; x++)
        {
            for (int z = minBound; z < maxBound; z++)
            {
                // Пропускаємо основну зону
                if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                    continue;

                // Створюємо траву
                Vector3 position = new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject grassTile = Instantiate(grassTilePrefab, position, Quaternion.identity, grassParent);
                grassTile.name = $"Grass_{x}_{z}";
                grassCount++;
            }
        }

        Debug.Log($"Створено {grassCount} тайлів трави");
    }

    private void PlacePlants()
    {
        if (plantPrefabs == null || plantPrefabs.Length == 0)
        {
            Debug.LogWarning("Немає префабів рослин!");
            return;
        }

        // Границі розширеної зони
        int minBound = -borderSize;
        int maxBound = gridSize + borderSize;

        int plantCount = 0;

        // Розміщуємо рослини
        for (int x = minBound; x < maxBound; x++)
        {
            for (int z = minBound; z < maxBound; z++)
            {
                // Пропускаємо основну зону
                if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                    continue;

                // Перевіряємо ймовірність
                if (Random.value > plantDensity)
                    continue;

                // Вибираємо випадкову рослину
                GameObject plantPrefab = plantPrefabs[Random.Range(0, plantPrefabs.Length)];

                // Випадкова позиція в межах клітинки
                float offsetX = Random.Range(-tileSize * 0.3f, tileSize * 0.3f);
                float offsetZ = Random.Range(-tileSize * 0.3f, tileSize * 0.3f);

                Vector3 position = new Vector3(
                    x * tileSize + offsetX,
                    0f,
                    z * tileSize + offsetZ
                );

                // Випадковий поворот
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, maxRotationY), 0);

                // Створюємо рослину
                GameObject plant = Instantiate(plantPrefab, position, rotation, plantsParent);

                // Випадковий масштаб
                float scale = Random.Range(minScale, maxScale);
                plant.transform.localScale = Vector3.one * scale;

                plant.name = $"Plant_{plantPrefab.name}_{x}_{z}";
                plantCount++;
            }
        }

        Debug.Log($"Розміщено {plantCount} рослин");
    }

    public void ClearDecoration()
    {
        if (grassParent != null)
            DestroyImmediate(grassParent.gameObject);
        if (plantsParent != null)
            DestroyImmediate(plantsParent.gameObject);
    }

    public void RegenerateDecoration()
    {
        ClearDecoration();
        GenerateTerrainDecoration();
    }

    // Для виклику з UI
    public void SetPlantDensity(float density)
    {
        plantDensity = Mathf.Clamp01(density);
    }
}