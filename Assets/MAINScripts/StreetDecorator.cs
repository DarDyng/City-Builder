using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StreetDecorator : MonoBehaviour
{
    [Header("Decor Prefabs")]
    [SerializeField] private GameObject[] benchPrefabs;      // �����
    [SerializeField] private GameObject[] conePrefabs;       // ������
    [SerializeField] private GameObject[] signPrefabs;       // �����
    [SerializeField] private GameObject[] streetTreePrefabs; // ������

    [Header("Spawn Settings")]
    [SerializeField][Range(0f, 1f)] private float decorDensity = 0.3f; // �������� �������� ������
    [SerializeField] private float edgeOffset = 0.4f; // ³����� �� ������ ������ (0.4 = 40% ������)
    [SerializeField] private float decorHeight = 0.1f; // ������ ���������

    [Header("Specific Settings")]
    [SerializeField][Range(0f, 1f)] private float benchNearBuildingChance = 0.7f; // ���� ����� ��� �������
    [SerializeField][Range(0f, 1f)] private float signAtIntersectionChance = 0.8f; // ���� ����� �� ���������
    [SerializeField][Range(0f, 1f)] private float treeAlongRoadChance = 0.5f; // ���� ������ ������ ������
    [SerializeField] private float minDecorDistance = 5f; // ̳������� ������� �� �������

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private Transform decorParent;
    private List<Vector3> placedDecorPositions = new List<Vector3>();
    private float tileSize = 20f;

    // ���� ������
    private enum DecorType
    {
        Bench,
        Cone,
        Sign,
        Tree
    }

    void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
        {
            tileSize = gridManager.TileSize;
            gridManager.OnTileChanged += OnTileChanged;
        }

        decorParent = new GameObject("StreetDecor").transform;
        decorParent.parent = transform;

        // ��������� �����������
        DecorateStreets();
    }

    public void DecorateStreets()
    {
        ClearDecor();
        placedDecorPositions.Clear();

        Debug.Log("�������� ����������� ������...");

        // ��������� �� ��� ����
        for (int x = 0; x < gridManager.GridWidth; x++)
        {
            for (int z = 0; z < gridManager.GridHeight; z++)
            {
                TileData tile = gridManager.GetTileData(x, z);

                if (tile.type == TileType.Road)
                {
                    DecorateRoadTile(x, z);
                }
            }
        }

        Debug.Log($"�������� {placedDecorPositions.Count} ������������ ��'����");
    }

    private void DecorateRoadTile(int x, int z)
    {
        // �������� ����� �����
        List<Vector2Int> roadNeighbors = new List<Vector2Int>();
        List<Vector2Int> buildingNeighbors = new List<Vector2Int>();
        List<Vector2Int> emptyNeighbors = new List<Vector2Int>();

        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // ϳ���
            new Vector2Int(1, 0),   // ����
            new Vector2Int(0, -1),  // ϳ�����
            new Vector2Int(-1, 0)   // �����
        };

        foreach (Vector2Int dir in directions)
        {
            int nx = x + dir.x;
            int nz = z + dir.y;

            if (gridManager.IsValidPosition(nx, nz))
            {
                TileData neighbor = gridManager.GetTileData(nx, nz);

                if (neighbor.type == TileType.Road)
                    roadNeighbors.Add(dir);
                else if (neighbor.type == TileType.Building)
                    buildingNeighbors.Add(dir);
                else
                    emptyNeighbors.Add(dir);
            }
        }

        // ��������� ��� �������
        bool isIntersection = roadNeighbors.Count >= 3;
        bool nearBuilding = buildingNeighbors.Count > 0;
        bool isRoadEdge = emptyNeighbors.Count > 0 || buildingNeighbors.Count > 0;

        if (!isRoadEdge) return; // �������� ����� ��� ����

        // �������� ��� ������ �� ����� �������
        DecorType? decorType = ChooseDecorType(isIntersection, nearBuilding, roadNeighbors.Count);

        if (decorType.HasValue && Random.value < decorDensity)
        {
            // �������� ����� �� �����
            PlaceDecorOnEdges(x, z, decorType.Value, emptyNeighbors.Concat(buildingNeighbors).ToList());
        }
    }

    private DecorType? ChooseDecorType(bool isIntersection, bool nearBuilding, int roadCount)
    {
        // ��������� ���������
        if (isIntersection && Random.value < signAtIntersectionChance && signPrefabs.Length > 0)
            return DecorType.Sign;

        if (nearBuilding && Random.value < benchNearBuildingChance && benchPrefabs.Length > 0)
            return DecorType.Bench;

        if (roadCount == 2 && Random.value < treeAlongRoadChance && streetTreePrefabs.Length > 0)
            return DecorType.Tree;

        // ���������� ���� � �����
        List<DecorType> availableTypes = new List<DecorType>();

        if (conePrefabs.Length > 0) availableTypes.Add(DecorType.Cone);
        if (streetTreePrefabs.Length > 0) availableTypes.Add(DecorType.Tree);
        if (!nearBuilding && benchPrefabs.Length > 0) availableTypes.Add(DecorType.Bench);

        if (availableTypes.Count > 0)
            return availableTypes[Random.Range(0, availableTypes.Count)];

        return null;
    }

    private void PlaceDecorOnEdges(int x, int z, DecorType decorType, List<Vector2Int> edgeDirections)
    {
        foreach (Vector2Int edgeDir in edgeDirections)
        {
            // ���������� ������� �� ���� ������
            Vector3 roadCenter = new Vector3(x * tileSize, decorHeight, z * tileSize);
            Vector3 edgeOffset3D = new Vector3(edgeDir.x, 0, edgeDir.y) * (tileSize * edgeOffset);
            Vector3 decorPosition = roadCenter + edgeOffset3D;

            // ������ ��������� �������
            decorPosition += new Vector3(
                Random.Range(-2f, 2f),
                0,
                Random.Range(-2f, 2f)
            );

            // ���������� �������� �������
            if (IsTooCloseToOtherDecor(decorPosition))
                continue;

            // �������� ������
            GameObject prefab = GetRandomPrefab(decorType);
            if (prefab == null) continue;

            // ��������� �����
            GameObject decor = Instantiate(prefab, decorPosition, Quaternion.identity, decorParent);

            // ���������� �������
            float randomRotation = Random.Range(0f, 360f);

            // ��� ����� �� ����� - ������� �� ������
            if (decorType == DecorType.Bench || decorType == DecorType.Sign)
            {
                Vector3 lookDirection = -edgeOffset3D; // ����������: ������������� edgeOffset3D
                if (lookDirection != Vector3.zero)
                {
                    decor.transform.rotation = Quaternion.LookRotation(lookDirection);
                    decor.transform.Rotate(0, Random.Range(-15f, 15f), 0); // �������� ���������
                }
            }
            else
            {
                decor.transform.rotation = Quaternion.Euler(0, randomRotation, 0);
            }

            // ���������� �������
            float scale = Random.Range(0.8f, 1.2f);
            decor.transform.localScale = Vector3.one * scale;

            // �������� �������
            placedDecorPositions.Add(decorPosition);

            // ������ ������ �� ���� ���������
            if (Random.value > 0.3f) break;
        }
    }

    private GameObject GetRandomPrefab(DecorType decorType)
    {
        GameObject[] prefabArray = decorType switch
        {
            DecorType.Bench => benchPrefabs,
            DecorType.Cone => conePrefabs,
            DecorType.Sign => signPrefabs,
            DecorType.Tree => streetTreePrefabs,
            _ => null
        };

        if (prefabArray != null && prefabArray.Length > 0)
        {
            return prefabArray[Random.Range(0, prefabArray.Length)];
        }

        return null;
    }

    private bool IsTooCloseToOtherDecor(Vector3 position)
    {
        foreach (Vector3 existingPos in placedDecorPositions)
        {
            if (Vector3.Distance(position, existingPos) < minDecorDistance)
                return true;
        }
        return false;
    }

    private void ClearDecor()
    {
        if (decorParent != null)
        {
            while (decorParent.childCount > 0)
            {
                DestroyImmediate(decorParent.GetChild(0).gameObject);
            }
        }
    }

    private void OnTileChanged(Vector2Int position, TileType newType)
    {
        // �������������� ����� ��� ��� ����
        if (newType == TileType.Road || newType == TileType.Ground)
        {
            // �������� ��� ���� ��� �� ��������� ����
            Invoke(nameof(DecorateStreets), 0.5f);
        }
    }

    public void SetDecorDensity(float density)
    {
        decorDensity = Mathf.Clamp01(density);
        DecorateStreets();
    }

    void OnDestroy()
    {
        if (gridManager != null)
        {
            gridManager.OnTileChanged -= OnTileChanged;
        }
    }
}