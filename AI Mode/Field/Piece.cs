using System;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    [Serializable]
    public class PieceBase
    {
        public Vector3 cubeCoords;
        public GameObject cubeObject;
    }

    [SerializeField] private int id;
    [SerializeField] private Material material;
    [SerializeField] private PieceBase[] pieceBase;

    private int minX, maxX, minY, maxY, minZ, maxZ;
    private HashSet<Vector3> baseCubes = new HashSet<Vector3>();
    private Dictionary<Vector3, GameObject> cubes = new Dictionary<Vector3, GameObject>();

    public int ID { get => id; }
    public int LenX { get => maxX - minX + 1; }
    public int LenY { get => maxY - minY + 1; }
    public int LenZ { get => maxZ - minZ + 1; }
    public Material Material { get => material; }

    public int MinX { get => minX; }
    public int MaxX { get => maxX; }
    public int MinY { get => minY; }
    public int MaxY { get => maxY; }
    public int MinZ { get => minZ; }
    public int MaxZ { get => maxZ; }

    private void Awake()
    {
        foreach (PieceBase piece in pieceBase)
        {
            baseCubes.Add(piece.cubeCoords);
            cubes.Add(piece.cubeCoords, piece.cubeObject);
        }
        RecalculateSize();
    }

    private void RecalculateSize()
    {
        minX = maxX = minY = maxY = minZ = maxZ = 0;

        foreach (Vector3 coords in cubes.Keys)
        {
            if (coords.x < minX) minX = (int)coords.x;
            else if (coords.x > maxX) maxX = (int)coords.x;

            if (coords.y < minY) minY = (int)coords.y;
            else if (coords.y > maxY) maxY = (int)coords.y;

            if (coords.z < minZ) minZ = (int)coords.z;
            else if (coords.z > maxZ) maxZ = (int)coords.z;
        }
    }

    public void AddCube(GameObject addCube, Vector3 cubeCoords)
    {
        if (cubes.ContainsKey(cubeCoords))
        {
            Debug.LogError($"‘игурка уже содержит куб по координатам {cubeCoords}");
            return;
        }

        GameObject newCube = Instantiate(addCube, gameObject.transform);
        newCube.transform.localPosition = cubeCoords;
        newCube.name = $"AddedCube({cubeCoords.x}, {cubeCoords.y}, {cubeCoords.z})";

        cubes.Add(cubeCoords, newCube);
        RecalculateSize();
    }

    public void DeleteCube(Vector3 cubeCoords)
    {
        if (!cubes.ContainsKey(cubeCoords))
        {
            Debug.LogError($"‘игурка не содержит куб по координатам {cubeCoords}");
            return;
        }
        
        if (baseCubes.Contains(cubeCoords))
        {
            Debug.LogError($"ѕопытка удалить базовый куб {cubeCoords}");
            return;
        }

        Destroy(cubes[cubeCoords]);
        cubes.Remove(cubeCoords);
        RecalculateSize();
    }

    private HashSet<Vector3> GetNeighbos(Vector3 origin, Vector3 deletedCube, bool skipBase = true, HashSet<Vector3> skip = null)
    {
        if (skip == null) skip = new HashSet<Vector3>();

        Vector3 neighborCube;
        HashSet<Vector3> neighborCubes = new HashSet<Vector3>();

        neighborCube = origin + new Vector3(-1, 0, 0);
        if (neighborCube != deletedCube && cubes.ContainsKey(neighborCube) && (!baseCubes.Contains(neighborCube) || !skipBase) && !skip.Contains(neighborCube)) 
            neighborCubes.Add(neighborCube);

        neighborCube = origin + new Vector3(1, 0, 0);
        if (neighborCube != deletedCube && cubes.ContainsKey(neighborCube) && (!baseCubes.Contains(neighborCube) || !skipBase) && !skip.Contains(neighborCube))
            neighborCubes.Add(neighborCube);

        neighborCube = origin + new Vector3(0, -1, 0);
        if (neighborCube != deletedCube && cubes.ContainsKey(neighborCube) && (!baseCubes.Contains(neighborCube) || !skipBase) && !skip.Contains(neighborCube))
            neighborCubes.Add(neighborCube);

        neighborCube = origin + new Vector3(0, 1, 0);
        if (neighborCube != deletedCube && cubes.ContainsKey(neighborCube) && (!baseCubes.Contains(neighborCube) || !skipBase) && !skip.Contains(neighborCube))
            neighborCubes.Add(neighborCube);

        neighborCube = origin + new Vector3(0, 0, -1);
        if (neighborCube != deletedCube && cubes.ContainsKey(neighborCube) && (!baseCubes.Contains(neighborCube) || !skipBase) && !skip.Contains(neighborCube))
            neighborCubes.Add(neighborCube);

        neighborCube = origin + new Vector3(0, 0, 1);
        if (neighborCube != deletedCube && cubes.ContainsKey(neighborCube) && (!baseCubes.Contains(neighborCube) || !skipBase) && !skip.Contains(neighborCube))
            neighborCubes.Add(neighborCube);

        return neighborCubes;
    }

    private HashSet<GameObject> WeakSpots(Vector3 origin, Vector3 deletedCube)
    {
        HashSet<Vector3> cantBeDeleted = new HashSet<Vector3>() { origin };
        HashSet<GameObject> weakSpots = new HashSet<GameObject>();

        Queue<Vector3> neighborCubes = new Queue<Vector3>(GetNeighbos(origin, deletedCube, false));
        while (neighborCubes.Count > 0)
        {
            Vector3 currentCube = neighborCubes.Dequeue();
            if (baseCubes.Contains(currentCube)) return weakSpots;

            cantBeDeleted.Add(currentCube);
            var temp = GetNeighbos(currentCube, deletedCube, false, cantBeDeleted);
            foreach (Vector3 cube in temp)
                neighborCubes.Enqueue(cube);
        }

        foreach (Vector3 weakSpotPos in cantBeDeleted)
            weakSpots.Add(cubes[weakSpotPos]);

        return weakSpots;
    }

    public bool CubeCanBeAdded(Vector3 cubeCoords, int maxLenX, int maxLenY, int maxLenZ)
    {
        if (LenX >= maxLenX)
            if (cubeCoords.x > maxX || cubeCoords.x < minX) return false;

        if (LenY >= maxLenY)
            if (cubeCoords.y > maxY || cubeCoords.y < minY) return false;

        if (LenZ >= maxLenZ)
            if (cubeCoords.z > maxZ || cubeCoords.z < minZ) return false;

        return true;
    }

    public bool CubeCanBeDeleted(Vector3 cubeCoords, out HashSet<GameObject> weakSpots)
    {
        weakSpots = new HashSet<GameObject>();

        if (baseCubes.Contains(cubeCoords)) return false;

        HashSet<Vector3> neighborCubes = GetNeighbos(cubeCoords, Vector3.zero);
        foreach (Vector3 neighborCube in neighborCubes)
        {
            weakSpots.UnionWith(WeakSpots(neighborCube, cubeCoords));
        }

        if (weakSpots.Count == 0) return true;
        else return false;
    }

    public HashSet<Vector3> FormatCubesCoords(Vector3 pieceCoord, Quaternion pieceRotation)
    {
        if (cubes.Count == 0) Debug.LogError("Zero cubes in prefab!");

        HashSet<Vector3> formatedCoords = new HashSet<Vector3>();
        foreach (Vector3 coords in cubes.Keys)
        {
            Vector3 temp = pieceRotation * coords + pieceCoord;
            temp.x = Mathf.Round(temp.x);
            temp.y = Mathf.Round(temp.y);
            temp.z = Mathf.Round(temp.z);

            formatedCoords.Add(temp);
        }
        return formatedCoords;
    }
}
