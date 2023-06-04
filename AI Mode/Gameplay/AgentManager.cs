using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AgentManager : Agent
{
    public struct MoveData
    {
        public float X { get; private set; }
        public float Z { get; private set; }
        public Vector3 Rotation { get; private set; }

        public MoveData(float x, float z, Vector3 rotation)
        {
            X = x;
            Z = z;
            Rotation = rotation;
        }
    }

    private readonly List<Vector3> anglesList = CalculateAngles();

    private enum WeightsTypes{
        clearedPlanes,
        bumpiness,
        maxHeight,
        minHeight
    }

    private bool actionReceived = false;
    private readonly Dictionary<WeightsTypes, float> weights = new Dictionary<WeightsTypes, float>()
    {
        { WeightsTypes.clearedPlanes, 0f },
        { WeightsTypes.bumpiness, 0f },
        { WeightsTypes.maxHeight, 0f },
        { WeightsTypes.minHeight, 0f },
    };
    private int currentPossibleClearedPlanes;
    private int currentHoles;
    private int currentBumpiness;
    private int currentMaxHeight;
    private int currentMinHeight;

    public bool EasyMode { get; set; } = false;
    public MoveData BestMove { get; private set; }
    public Queue<MoveData> TopBestMoves { get; private set; } = new Queue<MoveData>();

    public override void CollectObservations(VectorSensor sensor)
    {
        actionReceived = false;

        sensor.AddObservation(currentPossibleClearedPlanes);
        sensor.AddObservation(currentHoles);
        sensor.AddObservation(currentBumpiness);
        sensor.AddObservation(currentMaxHeight);
        sensor.AddObservation(currentMinHeight);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        weights[WeightsTypes.clearedPlanes] = (EasyMode) ? 100f : actions.ContinuousActions[0];
        weights[WeightsTypes.bumpiness] = actions.ContinuousActions[1];
        weights[WeightsTypes.maxHeight] = actions.ContinuousActions[2];
        weights[WeightsTypes.minHeight] = actions.ContinuousActions[3];

        actionReceived = true;
    }

    private static List<Vector3> CalculateAngles()
    {
        List<Vector3> anglesList = new List<Vector3>();

        for (int z = -90; z <= 180; z += 90)
            for (int y = -90; y <= 180; y += 90)
                for (int x = -90; x <= 180; x += 90)
                {
                    Vector3 vectorA = new Vector3(x, y, z);
                    Vector3 vectorB = new Vector3(Normalize(x + 180), Normalize(y + 180), Normalize(z + 180));

                    if (!anglesList.Contains(vectorA) && !anglesList.Contains(vectorB))
                    {
                        if (RotationLength(vectorA) < RotationLength(vectorB))
                            anglesList.Add(vectorA);
                        else
                            anglesList.Add(vectorB);
                    }
                }

        return anglesList;
    }

    private static float RotationLength(Vector3 angles) => Mathf.Abs(angles.x) + Mathf.Abs(angles.y) + Mathf.Abs(angles.z);

    private static int Normalize(int angle)
    {
        while (angle >= 270) angle -= 360;
        while (angle < -90) angle += 360;
        return angle;
    }

    private static int[,] CalculateHeights(ref bool[,,] field)
    {
        int[,] heights = new int[5, 5];

        for (int z = 0; z < 5; z++)
        {
            for (int x = 0; x < 5; x++)
            {
                int y;
                for (y = 9; y >= 0; y--)
                {
                    if (field[x, y, z]) break;
                }

                heights[x, z] = y + 1;
            }
        }

        return heights;
    }

    private int GetMaxClearedPlanes(GameObject[,,] field, Piece piece)
    {
        int maxClearedPlanes = 0;

        object locker = new object();
        Parallel.For(0, 5, x =>
        {
            Parallel.For(0, 5, z =>
            {
                Parallel.ForEach(anglesList, rotation =>
                {
                    if (!GetNewField(ref field, piece, x, z, Quaternion.Euler(rotation), out bool[,,] newField, out int clearedPlanes))
                    {
                        lock (locker)
                        {
                            if (clearedPlanes > maxClearedPlanes)
                            {
                                maxClearedPlanes = clearedPlanes;
                            }
                        }
                    }
                });
            });
        });

        return maxClearedPlanes;
    }

    private bool GetPrediction(ref GameObject[,,] field, Piece piece, int posX, int posZ, Quaternion rot,
        out int clearedPlanes, out int holes, out int bumpiness, out int maxHeight, out int minHeight)
    {
        holes = 0;
        bumpiness = 0;
        maxHeight = 0;
        minHeight = 11;

        if (!GetNewField(ref field, piece, posX, posZ, rot, out bool[,,] newField, out clearedPlanes)) return false;

        GetParameters(ref newField, out holes, out bumpiness, out maxHeight, out minHeight);
        return true;
    }

    private static void GetParameters(ref bool[,,] field,
        out int holes, out int bumpiness, out int maxHeight, out int minHeight)
    {
        holes = 0;
        bumpiness = 0;
        maxHeight = 0;
        minHeight = 11;

        int[,] heights = CalculateHeights(ref field);

        for (int x = 0; x < 5; x++)
        {
            for (int z = 0; z < 5; z++)
            {
                // Holes
                for (int y = heights[x, z] - 1; y >= 0; y--)
                    if (!field[x, y, z]) holes++;

                // Bumpiness
                if (x < 4) bumpiness += math.abs(heights[x, z] - heights[x + 1, z]);
                if (z < 4) bumpiness += math.abs(heights[x, z] - heights[x, z + 1]);

                // Max & Min
                if (heights[x, z] > maxHeight) maxHeight = heights[x, z];
                if (heights[x, z] < minHeight) minHeight = heights[x, z];
            }
        }
    }

    private bool GetNewField(ref GameObject[,,] field, Piece piece, int posX, int posZ, Quaternion rot,
        out bool[,,] newField, out int clearedPlanes)
    {
        newField = new bool[5, 10, 5];
        clearedPlanes = 0;

        HashSet<Vector3> pieceCoords = piece.FormatCubesCoords(new Vector3(posX, 0, posZ), rot);
        foreach (Vector3 cube in pieceCoords)
        {
            if ((int)cube.x < 0 || (int)cube.x > 4 || (int)cube.z < 0 || (int)cube.z > 4) return false;
        }

        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 10; y++)
                for (int z = 0; z < 5; z++)
                    newField[x, y, z] = (field[x, y, z] != null);

        int posY;
        for (posY = 12; posY >= 0; posY--)
        {
            bool overlay = false;

            pieceCoords = piece.FormatCubesCoords(new Vector3(posX, posY, posZ), rot);
            foreach (Vector3 cube in pieceCoords)
                if ((int)cube.y < 10 && (int)cube.y >= 0 && newField[(int)cube.x, (int)cube.y, (int)cube.z] || (int)cube.y < 0)
                {
                    overlay = true;
                    break;
                }

            if (overlay)
            {
                posY++;
                break;
            }
        }
        if (posY < 0) posY = 0;

        pieceCoords = piece.FormatCubesCoords(new Vector3(posX, posY, posZ), rot);
        foreach (Vector3 cube in pieceCoords)
        {
            if ((int)cube.y >= 10) return false;
            newField[(int)cube.x, (int)cube.y, (int)cube.z] = true;
        }

        int fullPlane;
        while ((fullPlane = GetFullPlane(ref newField)) != -1)
        {
            clearedPlanes++;

            for (int y = fullPlane; y < 9; y++)
                for (int x = 0; x < 5; x++)
                    for (int z = 0; z < 5; z++)
                    {
                        newField[x, y, z] = newField[x, y + 1, z];
                    }

            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                    newField[x, 9, z] = false;
        }

        return true;
    }

    private static int GetFullPlane(ref bool[,,] newField)
    {
        for (int y = 0; y < 10; y++)
        {
            bool full = true;
            bool empty = true;

            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                {
                    if (full && !newField[x, y, z])
                        full = false;

                    if (empty && newField[x, y, z])
                        empty = false;
                }

            if (empty) return -1;
            else if (full) return y;
        }

        return -1;
    }

    public IEnumerator CalculateMove(GameObject[,,] field, Piece currentPiece, int nBest)
    {
        currentPossibleClearedPlanes = GetMaxClearedPlanes(field, currentPiece);

        bool[,,] currentFieldBool = new bool[5, 10, 5];
        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 10; y++)
                for (int z = 0; z < 5; z++)
                    currentFieldBool[x, y, z] = (field[x, y, z] != null);

        GetParameters(ref currentFieldBool, out currentHoles, out currentBumpiness, out currentMaxHeight, out currentMinHeight);

        RequestDecision();
        while (!actionReceived) yield return null;

        float bestResult = float.MinValue;
        float bestX = 2;
        float bestZ = 2;
        Vector3 bestRotation = Vector3.zero;
        TopBestMoves.Clear();

        object locker = new object();
        Parallel.For(0, 5, x =>
        {
            Parallel.For(0, 5, z =>
            {
                Parallel.ForEach(anglesList, rotation =>
                {
                    if (GetPrediction(ref field, currentPiece, x, z, Quaternion.Euler(rotation),
                        out int clearedPlanes, out int holes, out int bumpiness, out int maxHeight, out int minHeight))
                    {
                        float result = clearedPlanes * weights[WeightsTypes.clearedPlanes] +
                                    holes * -10f +
                                    bumpiness * weights[WeightsTypes.bumpiness] +
                                    maxHeight * weights[WeightsTypes.maxHeight] +
                                    minHeight * weights[WeightsTypes.minHeight];

                        lock (locker)
                        {
                            if (result > bestResult)
                            {
                                bestResult = result;
                                bestX = x;
                                bestZ = z;
                                bestRotation = rotation;

                                TopBestMoves.Enqueue(new MoveData(bestX, bestZ, bestRotation));
                                if (TopBestMoves.Count > nBest) TopBestMoves.Dequeue();
                            }
                        }
                    }
                });
            });
        });

        BestMove = new MoveData(bestX, bestZ, bestRotation);
    }
}
