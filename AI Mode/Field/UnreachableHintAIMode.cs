using System.Collections.Generic;
using UnityEngine;

public class UnreachableHintAIMode : MonoBehaviour
{
    [SerializeField] private GameObject unreachablePrefab = null;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private readonly HashSet<GameObject> usedHintObjects = new HashSet<GameObject>();

    private void Start()
    {
        for (int i = 0; i < 250; i++)
        {
            GameObject hintObject = Instantiate(unreachablePrefab, transform);
            hintObject.SetActive(false);
            pool.Enqueue(hintObject);
        }
    }

    private GameObject GetHintObject()
    {
        GameObject hintObject;

        if (pool.Count > 0)
        {
            hintObject = pool.Dequeue();
            hintObject.SetActive(true);
        }
        else
        {
            hintObject = Instantiate(unreachablePrefab, transform);
        }
        usedHintObjects.Add(hintObject);

        return hintObject;
    }

    private void ResetHintObjects()
    {
        foreach (GameObject hintObject in usedHintObjects)
        {
            hintObject.SetActive(false);
            hintObject.transform.localPosition = new Vector3(0, 0, 0);
            pool.Enqueue(hintObject);
        }
        usedHintObjects.Clear();
    }

    public void SetUnreachableHint(ref GameObject[,,] field, Vector3 offset)
    {
        ResetHintObjects();

        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 5; z++)
            {
                bool foundCube = false;
                for (int y = 9; y >= 0; y--)
                {
                    if (foundCube && field[x, y, z] == null)
                    {
                        GameObject hintObject = GetHintObject();
                        hintObject.transform.position = new Vector3(x, y, z) + offset;
                    }
                    else if (!foundCube && field[x, y, z] != null) foundCube = true;
                }
            }
        }
    }
}
