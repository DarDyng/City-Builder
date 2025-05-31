using System.Collections;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    private GridManager gridManager;

    void Start()
    {
        // ������ ���� GridManager ������������
        StartCoroutine(DelayedGeneration());
    }

    private System.Collections.IEnumerator DelayedGeneration()
    {
        // ������ ����� ����� ��� GridManager ����� �����������
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
            Debug.Log("��� ����������� ����� GridManager!");
        }
        else
        {
            Debug.LogError("GridManager �� ��������!");
        }
    }

    public void RegenerateWorld()
    {
        if (gridManager != null)
        {
            gridManager.GenerateWorld();
            Debug.Log("��� ��������������!");
        }
    }
}