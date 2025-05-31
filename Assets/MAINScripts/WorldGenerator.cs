using System.Collections;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    private GridManager gridManager;

    void Start()
    {
        // Чекаємо поки GridManager ініціалізується
        StartCoroutine(DelayedGeneration());
    }

    private System.Collections.IEnumerator DelayedGeneration()
    {
        // Чекаємо кілька кадрів щоб GridManager встиг запуститися
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        gridManager = GetComponent<GridManager>();
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (gridManager != null)
        {
            gridManager.GenerateWorld();
            Debug.Log("Світ згенеровано через GridManager!");
        }
        else
        {
            Debug.LogError("GridManager не знайдено!");
        }
    }

    public void RegenerateWorld()
    {
        if (gridManager != null)
        {
            gridManager.GenerateWorld();
            Debug.Log("Світ перегенеровано!");
        }
    }
}