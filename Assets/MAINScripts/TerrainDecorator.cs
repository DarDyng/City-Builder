using UnityEngine;
using System.Collections.Generic;

public class TerrainDecorator : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private int borderSize = 20; // ����� ������� ������� ������� ����
    [SerializeField] private float tileSize = 20f; // ����� ���� �������

    [Header("Prefabs")]
    [SerializeField] private GameObject grassTilePrefab; // ������ �����
    [SerializeField] private GameObject[] plantPrefabs; // ����� ������� ������

    [Header("Plant Settings")]
    [SerializeField][Range(0f, 1f)] private float plantDensity = 0.8f; // ��������� ����� �������
    [SerializeField] private float minScale = 0.8f; // ̳�������� ������� �������
    [SerializeField] private float maxScale = 1.2f; // ������������ ������� �������
    [SerializeField] private float maxRotationY = 360f; // ������������ �������

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private Transform grassParent;
    private Transform plantsParent;
    private int gridSize = 20; // ����� ������� ����

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
        Debug.Log("�������� ��������� ���������� ��������...");

        // ��������� ��������� ��'����
        CreateParentObjects();

        // �������� �����
        GenerateGrassBorder();

        // �������� �������
        PlacePlants();

        Debug.Log("��������� ���������� �������� ���������!");
    }

    private void CreateParentObjects()
    {
        // ��������� ���� ��'���� ���� �
        if (grassParent != null)
            DestroyImmediate(grassParent.gameObject);
        if (plantsParent != null)
            DestroyImmediate(plantsParent.gameObject);

        // ��������� ���
        grassParent = new GameObject("TerrainGrass").transform;
        plantsParent = new GameObject("TerrainPlants").transform;

        grassParent.parent = transform;
        plantsParent.parent = transform;
    }

    private void GenerateGrassBorder()
    {
        if (grassTilePrefab == null)
        {
            Debug.LogError("Grass Tile Prefab �� ����������!");
            return;
        }

        // ������� ��������� ����
        int minBound = -borderSize;
        int maxBound = gridSize + borderSize;

        int grassCount = 0;

        // �������� ����� ������� ������� ����
        for (int x = minBound; x < maxBound; x++)
        {
            for (int z = minBound; z < maxBound; z++)
            {
                // ���������� ������� ����
                if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                    continue;

                // ��������� �����
                Vector3 position = new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject grassTile = Instantiate(grassTilePrefab, position, Quaternion.identity, grassParent);
                grassTile.name = $"Grass_{x}_{z}";
                grassCount++;
            }
        }

        Debug.Log($"�������� {grassCount} ����� �����");
    }

    private void PlacePlants()
    {
        if (plantPrefabs == null || plantPrefabs.Length == 0)
        {
            Debug.LogWarning("���� ������� ������!");
            return;
        }

        // ������� ��������� ����
        int minBound = -borderSize;
        int maxBound = gridSize + borderSize;

        int plantCount = 0;

        // �������� �������
        for (int x = minBound; x < maxBound; x++)
        {
            for (int z = minBound; z < maxBound; z++)
            {
                // ���������� ������� ����
                if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                    continue;

                // ���������� ���������
                if (Random.value > plantDensity)
                    continue;

                // �������� ��������� �������
                GameObject plantPrefab = plantPrefabs[Random.Range(0, plantPrefabs.Length)];

                // ��������� ������� � ����� �������
                float offsetX = Random.Range(-tileSize * 0.3f, tileSize * 0.3f);
                float offsetZ = Random.Range(-tileSize * 0.3f, tileSize * 0.3f);

                Vector3 position = new Vector3(
                    x * tileSize + offsetX,
                    0f,
                    z * tileSize + offsetZ
                );

                // ���������� �������
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, maxRotationY), 0);

                // ��������� �������
                GameObject plant = Instantiate(plantPrefab, position, rotation, plantsParent);

                // ���������� �������
                float scale = Random.Range(minScale, maxScale);
                plant.transform.localScale = Vector3.one * scale;

                plant.name = $"Plant_{plantPrefab.name}_{x}_{z}";
                plantCount++;
            }
        }

        Debug.Log($"�������� {plantCount} ������");
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

    // ��� ������� � UI
    public void SetPlantDensity(float density)
    {
        plantDensity = Mathf.Clamp01(density);
    }
}