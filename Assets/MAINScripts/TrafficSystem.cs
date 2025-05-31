using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrafficSystem : MonoBehaviour
{
    [Header("Traffic Settings")]
    [SerializeField] private GameObject[] carPrefabs;
    [SerializeField] private int maxCarsTotal = 20; // �������� ����� �������
    [SerializeField] private float carSpeed = 5f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float carHeight = 0.5f;
    [SerializeField] private float laneOffset = 0.25f; // ������� �� ������ ������

    [Header("Traffic Rules")]
    [SerializeField] private bool rightHandTraffic = true; // true = ������������� ���

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private Transform carsParent;
    private List<Car> activeCars = new List<Car>();
    private List<Vector2Int> roadPositions = new List<Vector2Int>();
    private Dictionary<Vector2Int, List<Vector2Int>> roadConnections = new Dictionary<Vector2Int, List<Vector2Int>>();

    private float tileSize = 20f;
    private float nextSpawnTime = 0f;

    private class Car
    {
        public GameObject gameObject;
        public Vector2Int currentTile;
        public Vector2Int targetTile;
        public Vector2Int previousTile;
        public float moveProgress = 0f;
        public int laneIndex; // 0 = ����� �����
        public Vector3 currentLaneOffset;
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

        carsParent = new GameObject("TrafficCars").transform;
        carsParent.parent = transform;

        ScanRoads();
    }

    void Update()
    {
        UpdateCars();

        // ����� ����� �����
        if (Time.time >= nextSpawnTime && roadPositions.Count > 1 && activeCars.Count < maxCarsTotal)
        {
            SpawnCar();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnCar()
    {
        if (carPrefabs == null || carPrefabs.Length == 0 || roadPositions.Count == 0)
            return;

        // �������� ��������� �������� �������
        Vector2Int startPos = roadPositions[Random.Range(0, roadPositions.Count)];

        // ���������� �� � ���� �����
        if (!roadConnections.ContainsKey(startPos) || roadConnections[startPos].Count == 0)
            return;

        // �������� ��������
        List<Vector2Int> possibleDirections = roadConnections[startPos];
        Vector2Int firstTarget = possibleDirections[Random.Range(0, possibleDirections.Count)];

        // ��������� ����� �� �������
        Vector2Int direction = firstTarget - startPos;
        Vector3 laneOffset = CalculateLaneOffset(direction, 0); // ������ ����� �����

        // ��������� ������
        GameObject carPrefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
        Vector3 worldPos = GetWorldPosition(startPos) + laneOffset;
        worldPos.y = carHeight;

        GameObject carObj = Instantiate(carPrefab, worldPos, Quaternion.identity, carsParent);

        Car car = new Car
        {
            gameObject = carObj,
            currentTile = startPos,
            targetTile = firstTarget,
            previousTile = startPos,
            laneIndex = 0,
            currentLaneOffset = laneOffset
        };

        activeCars.Add(car);

        // ������������ ���������� �������
        UpdateCarRotation(car, direction);
    }

    private Vector3 CalculateLaneOffset(Vector2Int direction, int laneIndex)
    {
        Vector3 dir3D = new Vector3(direction.x, 0, direction.y);
        Vector3 perpendicular = Vector3.Cross(Vector3.up, dir3D).normalized;

        // ��� ���������������� ���� ������ ������
        float offsetMultiplier = rightHandTraffic ? 1f : -1f;

        return perpendicular * (laneOffset * tileSize * offsetMultiplier);
    }

    private void UpdateCars()
    {
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            Car car = activeCars[i];

            // ���������� �� ������ �� ����
            if (!roadPositions.Contains(car.currentTile))
            {
                Destroy(car.gameObject);
                activeCars.RemoveAt(i);
                continue;
            }

            // ��� ������ � �������� �������� (��� �����������)
            if (car.currentTile != car.targetTile)
            {
                car.moveProgress += (carSpeed / tileSize) * Time.deltaTime;

                if (car.moveProgress >= 1f)
                {
                    // ������� ������� �������
                    car.previousTile = car.currentTile;
                    car.currentTile = car.targetTile;
                    car.moveProgress = 0f;

                    // ��������� �������� ����
                    FindNextTarget(car);
                }
                else
                {
                    // ������������ ������� � ����������� �����
                    UpdateCarPosition(car);
                }
            }
        }
    }

    private void UpdateCarPosition(Car car)
    {
        Vector3 startPos = GetWorldPosition(car.currentTile);
        Vector3 endPos = GetWorldPosition(car.targetTile);

        // ���������� ������� ��� �����
        Vector2Int currentDirection = car.targetTile - car.currentTile;
        Vector3 currentLaneOffset = CalculateLaneOffset(currentDirection, car.laneIndex);

        // ������ ���� ������� ����� ��� ���������
        car.currentLaneOffset = Vector3.Lerp(car.currentLaneOffset, currentLaneOffset, Time.deltaTime * 5f);

        // ������� � ����������� �����
        startPos += car.currentLaneOffset;
        endPos += currentLaneOffset;

        startPos.y = endPos.y = carHeight;

        // ������������ �������
        car.gameObject.transform.position = Vector3.Lerp(startPos, endPos, car.moveProgress);

        // ��������� ��������
        UpdateCarRotation(car, currentDirection);
    }

    private void UpdateCarRotation(Car car, Vector2Int direction)
    {
        Vector3 dir3D = new Vector3(direction.x, 0, direction.y);
        if (dir3D != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir3D);
            car.gameObject.transform.rotation = Quaternion.Slerp(
                car.gameObject.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f
            );
        }
    }

    private void FindNextTarget(Car car)
    {
        if (!roadConnections.ContainsKey(car.currentTile)) return;

        List<Vector2Int> possibleTargets = new List<Vector2Int>(roadConnections[car.currentTile]);

        // ��������� ������ � ��� ������� (��� �� ������������)
        possibleTargets.Remove(car.previousTile);

        if (possibleTargets.Count == 0)
        {
            // ����� - ������������
            possibleTargets.Add(car.previousTile);
        }

        if (possibleTargets.Count > 0)
        {
            // �������� �������� ������ � ����������� ��������� ������� ����
            Vector2Int currentDirection = car.currentTile - car.previousTile;
            Vector2Int preferredTarget = car.currentTile + currentDirection;

            if (possibleTargets.Contains(preferredTarget) && Random.value > 0.3f)
            {
                // 70% ���� ����� �����
                car.targetTile = preferredTarget;
            }
            else
            {
                // ���������� �������
                car.targetTile = possibleTargets[Random.Range(0, possibleTargets.Count)];
            }
        }
    }

    private void ScanRoads()
    {
        roadPositions.Clear();
        roadConnections.Clear();

        if (gridManager == null) return;

        // ��������� �� ������
        for (int x = 0; x < gridManager.GridWidth; x++)
        {
            for (int z = 0; z < gridManager.GridHeight; z++)
            {
                TileData tile = gridManager.GetTileData(x, z);
                if (tile.type == TileType.Road)
                {
                    Vector2Int pos = new Vector2Int(x, z);
                    roadPositions.Add(pos);
                }
            }
        }

        BuildRoadConnections();
        Debug.Log($"�������� {roadPositions.Count} ���� ��� ���� ����������");
    }

    private void BuildRoadConnections()
    {
        foreach (Vector2Int roadPos in roadPositions)
        {
            List<Vector2Int> connections = new List<Vector2Int>();

            Vector2Int[] directions = {
                new Vector2Int(0, 1),   // ϳ���
                new Vector2Int(1, 0),   // ����
                new Vector2Int(0, -1),  // ϳ�����
                new Vector2Int(-1, 0)   // �����
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = roadPos + dir;
                if (roadPositions.Contains(neighborPos))
                {
                    connections.Add(neighborPos);
                }
            }

            roadConnections[roadPos] = connections;
        }
    }

    private Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);
    }

    private void OnTileChanged(Vector2Int position, TileType newType)
    {
        ScanRoads();

        // ��������� ������ �� ��������� �������
        if (newType != TileType.Road)
        {
            for (int i = activeCars.Count - 1; i >= 0; i--)
            {
                if (activeCars[i].currentTile == position || activeCars[i].targetTile == position)
                {
                    Destroy(activeCars[i].gameObject);
                    activeCars.RemoveAt(i);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (gridManager != null)
        {
            gridManager.OnTileChanged -= OnTileChanged;
        }
    }

    // ������ ������ ��� UI
    public void SetCarSpeed(float speed)
    {
        carSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.5f, interval);
    }

    public void SetMaxCars(int maxCars)
    {
        maxCarsTotal = Mathf.Max(1, maxCars);
    }
}