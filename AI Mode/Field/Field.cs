using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Transform fieldObject;
    [SerializeField] private BGAnimationAIMode bgAnimation;
    [SerializeField] private FieldAnimationAIMode fieldAnimation;
    [SerializeField] private GameplayUI gameplayUI;

    [Header("Hint")]
    [SerializeField] private GameObject hintGameObject;
    [SerializeField] private Color[] hintOKColor;
    [ColorUsage(true, true)]
    [SerializeField] private Color hintErrorColor;
    [SerializeField] private UnreachableHintAIMode unreachableHint;

    private GameObject currentPieceObject;
    private Piece currentPieceClass;
    private Vector3 currentPiecePos;
    private Quaternion currentPieceRot;

    private GameObject currentHintObject;
    private Vector3 currentHintPos;
    private Quaternion currentHintRot;

    private GameObject[,,] field = new GameObject[5, 15, 5];

    private bool hintWasOK = true;

    public Piece CurrentPiece { get => currentPieceClass; }
    public GameObject[,,] FieldData { get => field; }
    public bool GameOver { get; private set; } = false;

    private void Start()
    {
        StartCoroutine(SmoothMovement());
    }

    public void SetUnreachableHint() => unreachableHint.SetUnreachableHint(ref field, transform.position);

    private IEnumerator SmoothMovement()
    {
        while (true)
        {
            if (GameOver) break;

            if (currentHintObject != null)
            {
                Vector3 tempPos = currentHintObject.transform.position;
                if (!tempPos.Equals(currentHintPos))
                {
                    if ((tempPos - currentHintPos).magnitude <= 0.05) currentHintObject.transform.position = currentHintPos;
                    else currentHintObject.transform.position = Vector3.Lerp(tempPos, currentHintPos, Time.deltaTime * 15.0f);
                }
                Quaternion tempRot = currentHintObject.transform.rotation;
                if (!tempRot.Equals(currentHintRot))
                {
                    if (Quaternion.Angle(tempRot, currentHintRot) <= 1.0f) currentHintObject.transform.rotation = currentHintRot;
                    else currentHintObject.transform.rotation = Quaternion.Lerp(tempRot, currentHintRot, Time.deltaTime * 15.0f);
                }
            }
            yield return 0;
        }
    }

    public void SpawnPiece(GameObject pieceObject)
    {
        currentPieceObject = pieceObject;
        currentPieceObject.SetActive(false);

        currentPieceClass = pieceObject.GetComponent<Piece>();

        currentPiecePos = new Vector3(2, 12 - currentPieceClass.MinY, 2) + transform.position;
        currentPieceRot = Quaternion.Euler(Vector3.zero);

        hintWasOK = true;
        currentHintObject = new GameObject("Field Hint");
        foreach (Transform pieceCube in pieceObject.transform)
        {
            GameObject hintCube = Instantiate(hintGameObject, currentHintObject.transform);
            hintCube.transform.localPosition = pieceCube.localPosition;
            MeshRenderer meshRenderer = hintCube.GetComponentInChildren<MeshRenderer>();
            meshRenderer.material.SetColor("_Base_Color", hintOKColor[currentPieceClass.ID]);
        }
        ShowHint();

        currentHintObject.transform.position = currentHintPos;
        currentHintObject.transform.rotation = currentHintRot;
    }

    private bool PieceCanBeMoved(HashSet<Vector3> formatedCoords, bool informIfError = true)
    {
        foreach (Vector3 cubeCoords in formatedCoords)
        {
            if (cubeCoords.y < 0.0f + transform.position.y) return false;
            else if (cubeCoords.x < 0.0f + transform.position.x)
            {
                if (informIfError)
                {
                    SoundsManager.PlaySound("Error");
                    bgAnimation.WallAndBGAnimationError(FieldBGAnimation.Wall.minX);
                    gameplayUI.ErrorEffect();
                }
                return false;
            }
            else if (cubeCoords.x > 4.0f + transform.position.x)
            {
                if (informIfError)
                {
                    SoundsManager.PlaySound("Error");
                    bgAnimation.WallAndBGAnimationError(FieldBGAnimation.Wall.maxX);
                    gameplayUI.ErrorEffect();
                }
                return false;
            }
            else if (cubeCoords.z < 0.0f + transform.position.z)
            {
                if (informIfError)
                {
                    SoundsManager.PlaySound("Error");
                    bgAnimation.WallAndBGAnimationError(FieldBGAnimation.Wall.minZ);
                    gameplayUI.ErrorEffect();
                }
                return false;
            }
            else if (cubeCoords.z > 4.0f + transform.position.z)
            {
                if (informIfError)
                {
                    SoundsManager.PlaySound("Error");
                    bgAnimation.WallAndBGAnimationError(FieldBGAnimation.Wall.maxZ);
                    gameplayUI.ErrorEffect();
                }
                return false;
            }
        }

        foreach (Vector3 cubeCoords in formatedCoords)
        {
            if (field[  (int)(cubeCoords.x - transform.position.x), 
                        (int)(cubeCoords.y - transform.position.y), 
                        (int)(cubeCoords.z - transform.position.z)] != null)
            {
                return false;
            }
        }

        return true;
    }

    private Vector3 OutOfBoundaryFix(Vector3 position, Quaternion rotation)
    {
        HashSet<Vector3> formatedCoords = currentPieceClass.FormatCubesCoords(position, rotation);

        float xMin, xMax, zMin, zMax;
        xMin = xMax = zMin = zMax = 0f;

        foreach(Vector3 cubePos in formatedCoords)
        {
            if (cubePos.x < 0 && xMin > cubePos.x) xMin = cubePos.x;
            else if (cubePos.x > 4 && xMax < (cubePos.x - 4)) xMax = cubePos.x - 4;

            if (cubePos.z < 0 && zMin > cubePos.z) zMin = cubePos.z;
            else if (cubePos.z > 4 && zMax < (cubePos.z - 4)) zMax = cubePos.z - 4;
        }

        return -new Vector3(xMax + xMin, 0, zMax + zMin);
    }

    public bool MovePieceSide(Vector3 moveCoord)
    {
        if (currentHintObject == null) return true;

        Vector3 newCoords = currentPiecePos + moveCoord;
        HashSet<Vector3> formatedCoords = currentPieceClass.FormatCubesCoords(newCoords, currentPieceRot);

        if (!PieceCanBeMoved(formatedCoords)) return false;

        SoundsManager.PlaySound("PieceMovement", Random.Range(0.8f, 1.2f), currentPiecePos);
        currentPiecePos += moveCoord;
        ShowHint();
        return true;
    }

    public bool RotatePiece(Vector3 rotationAngles)
    {
        if (currentHintObject == null) return true;

        Quaternion actualRot = currentHintObject.transform.rotation;
        currentHintObject.transform.rotation = currentPieceRot;

        currentHintObject.transform.Rotate(rotationAngles, Space.World);
        currentPieceRot = currentHintObject.transform.rotation;
        currentHintObject.transform.rotation = actualRot;

        currentPiecePos += OutOfBoundaryFix(currentPiecePos, currentPieceRot);

        SoundsManager.PlaySound("PieceRotation", Random.Range(0.75f, 1.25f), currentPiecePos);
        ShowHint();
        return true;
    }

    public void MovePieceCoord(float x, float z, Vector3 rotationAngles)
    {
        currentPieceRot = Quaternion.Euler(rotationAngles);
        currentPiecePos = new Vector3(x + transform.position.x, currentPiecePos.y, z + transform.position.z);
        SoundsManager.PlaySound("PieceMovement", Random.Range(0.8f, 1.2f), currentPiecePos);
        ShowHint();
    }

    public IEnumerator InstallPiece()
    {
        HashSet<Vector3> formatedCoords =
            currentPieceClass.FormatCubesCoords(currentHintPos, currentHintRot);

        HashSet<GameObject> cubes = new HashSet<GameObject>();

        foreach (Vector3 cubeCoords in formatedCoords)
        {
            GameObject cube = Instantiate(cubePrefab, fieldObject);
            cube.transform.position = cubeCoords;
            cube.GetComponent<MeshRenderer>().material = currentPieceClass.Material;
            field[  (int)(cubeCoords.x - transform.position.x),
                    (int)(cubeCoords.y - transform.position.y),
                    (int)(cubeCoords.z - transform.position.z)] = cube;
            cubes.Add(cube);

            if (cubeCoords.y > 9.0f + transform.position.y) GameOver = true;
        }

        SoundsManager.PlaySound("Appear");
        yield return StartCoroutine(fieldAnimation.PieceAppearanceAnimation(cubes, currentPieceClass));

        Destroy(currentHintObject);
        Destroy(currentPieceObject);
        currentPieceClass = null;
    }

    public int GetFullPlane()
    {
        for (int y = 0; y < 10; y++)
        {
            bool full = true;
            bool empty = true;

            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                {
                    if (full && field[x, y, z] == null)
                        full = false;

                    if (empty && field[x, y, z] != null)
                        empty = false;
                }

            if (empty) return -1;
            else if (full) return y;
        }

        return -1;
    }

    public IEnumerator DeletePlane(int startY)
    {
        if (startY >= 0)
        {
            HashSet<Material> deletedCubesMaterials = new HashSet<Material>();
            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                    deletedCubesMaterials.Add(field[x, startY, z].GetComponent<MeshRenderer>().material);

            bgAnimation.BGAnimationOK();
            SoundsManager.PlaySound("Dissolving");
            yield return fieldAnimation.DissolveCubes(deletedCubesMaterials);

            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                    Destroy(field[x, startY, z]);

            HashSet<Transform> dropedCubes = new HashSet<Transform>();
            for (int y = startY; y < 9; y++)
                for (int x = 0; x < 5; x++)
                    for (int z = 0; z < 5; z++)
                    {
                        GameObject currentCube = field[x, y + 1, z];
                        field[x, y, z] = currentCube;

                        if (currentCube != null)
                            dropedCubes.Add(currentCube.transform);
                    }

            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                    field[x, 9, z] = null;

            yield return fieldAnimation.DropCubes(dropedCubes);
        }
    }

    private void ShowHint()
    {
        Vector3 hintPos = currentPiecePos;

        while (PieceCanBeMoved(currentPieceClass.FormatCubesCoords(hintPos + Vector3.down, currentPieceRot), false))
            hintPos += Vector3.down;

        currentHintPos = hintPos;
        currentHintRot = currentPieceRot;

        HashSet<Vector3> hintPosSet = currentPieceClass.FormatCubesCoords(hintPos, currentPieceRot);
        bool hintIsOK = true;
        foreach (Vector3 hintCubePos in hintPosSet)
        {
            if (hintCubePos.y > 9.0f + transform.position.y)
            {
                hintIsOK = false;
                break;
            }
        }

        if (hintIsOK && !hintWasOK)
        {
            hintWasOK = true;
            foreach (Transform hintCube in currentHintObject.transform)
                hintCube.GetComponentInChildren<MeshRenderer>().material.SetColor("_Base_Color", hintOKColor[currentPieceClass.ID]);
        }
        else if (!hintIsOK && hintWasOK)
        {
            hintWasOK = false;
            foreach (Transform hintCube in currentHintObject.transform)
                hintCube.GetComponentInChildren<MeshRenderer>().material.SetColor("_Base_Color", hintErrorColor);
        }
    }
}
