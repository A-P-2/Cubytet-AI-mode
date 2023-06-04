using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldAnimationAIMode : MonoBehaviour
{
    public enum Cuts
    {
        xMin,
        xMax,
        zMin,
        zMax
    }

    [Header("Spawn Animation")]
    [SerializeField] private float pieceAppearanceStart = -0.6f;
    [SerializeField] private float pieceAppearanceEnd = 9.6f;
    [SerializeField] private float pieceAppearanceSpeed = 1.0f;

    [SerializeField] private GameObject cutYObject;
    [SerializeField] private Material cutYMaterial;
    [SerializeField] private float cutYEnd = 9.4f;
    [SerializeField] private float cutYAlphaSpeed = 1.0f;
    [SerializeField] private bool cutYMaterialManager = false;

    [Header("Deleting Animation")]
    [SerializeField] private float dissolveSpeed;
    [SerializeField] private float dropSpeed;

    [Header("Cut Animation")]
    [SerializeField] private bool cutManager = false;
    [SerializeField] private float cutSpeed;
    [SerializeField] private float defaultCutXMin;
    [SerializeField] private float defaultCutXMax;
    [SerializeField] private float defaultCutZMin;
    [SerializeField] private float defaultCutZMax;
    [SerializeField] private Material[] materialsPiece;
    [SerializeField] private Material materialCutHint;
    [SerializeField] private Material materialUnreachableHint;

    private bool cutActive = false;
    private float currentCutXMin;
    private float currentCutXMax;
    private float currentCutZMin;
    private float currentCutZMax;

    private void Start()
    {
        cutYObject.transform.localPosition = new Vector3(0, pieceAppearanceStart, 0);
        if (cutYMaterialManager) cutYMaterial.SetFloat("_Alpha", 0);

        if (cutManager)
        {
            currentCutXMin = defaultCutXMin;
            currentCutXMax = defaultCutXMax;
            currentCutZMin = defaultCutZMin;
            currentCutZMax = defaultCutZMax;

            foreach (Material materialPiece in materialsPiece)
            {
                materialPiece.SetFloat("Cut_X_Min", defaultCutXMin);
                materialPiece.SetFloat("Cut_X_Max", defaultCutXMax);
                materialPiece.SetFloat("Cut_Z_Min", defaultCutZMin);
                materialPiece.SetFloat("Cut_Z_Max", defaultCutZMax);
                materialPiece.SetInt("Cut_Active", 0);
            }

            materialCutHint.SetFloat("Cut_X_Min", defaultCutXMin);
            materialUnreachableHint.SetFloat("Cut_X_Min", defaultCutXMin);
            FieldWallAnimation.SetFloatForAll("Cut_X_Min", defaultCutXMin);

            materialCutHint.SetFloat("Cut_X_Max", defaultCutXMax);
            materialUnreachableHint.SetFloat("Cut_X_Max", defaultCutXMax);
            FieldWallAnimation.SetFloatForAll("Cut_X_Max", defaultCutXMax);

            materialCutHint.SetFloat("Cut_Z_Min", defaultCutZMin);
            materialUnreachableHint.SetFloat("Cut_Z_Min", defaultCutZMin);
            FieldWallAnimation.SetFloatForAll("Cut_Z_Min", defaultCutZMin);

            materialCutHint.SetFloat("Cut_Z_Max", defaultCutZMax);
            materialUnreachableHint.SetFloat("Cut_Z_Max", defaultCutZMax);
            FieldWallAnimation.SetFloatForAll("Cut_Z_Max", defaultCutZMax);

            StartCoroutine(CutAnimation());
        }
    }

    public IEnumerator PieceAppearanceAnimation(HashSet<GameObject> cubes, Piece pieceClass)
    {
        if (cutYMaterialManager) cutYMaterial.SetFloat("_Alpha", 1.0f);

        HashSet<Material> materials = new HashSet<Material>();
        foreach (GameObject cube in cubes)
            materials.Add(cube.GetComponent<MeshRenderer>().material);

        float cutY = pieceAppearanceStart;
        foreach (Material material in materials) material.SetFloat("_CutY", cutY);
        cutYObject.transform.position = new Vector3(0, cutY, 0);

        Coroutine cutYAlphaCoroutine = null;

        while (cutY < pieceAppearanceEnd)
        {
            yield return null;

            cutY += Time.deltaTime * pieceAppearanceSpeed;
            if (cutY > pieceAppearanceEnd) cutY = pieceAppearanceEnd;

            foreach(Material material in materials) material.SetFloat("_CutY", cutY);
            if (cutY <= cutYEnd) cutYObject.transform.localPosition = new Vector3(0, cutY, 0);
            else if (cutYMaterialManager) cutYAlphaCoroutine = StartCoroutine(CutYObjectDissolve());
        }

        foreach (GameObject cube in cubes) cube.GetComponent<MeshRenderer>().material = pieceClass.Material;

        if (cutYMaterialManager) yield return cutYAlphaCoroutine;
    }

    private IEnumerator CutYObjectDissolve()
    {
        float alpha = 1.0f;

        while (alpha > 0)
        {
            yield return null;

            alpha -= Time.deltaTime * cutYAlphaSpeed;
            if (alpha < 0) alpha = 0;

            cutYMaterial.SetFloat("_Alpha", alpha);
        }
    }

    public IEnumerator DissolveCubes(HashSet<Material> deletedCubesMaterials)
    {
        float t = 0.0001f;
        while (t < 1)
        {
            t += Time.deltaTime * dissolveSpeed;
            if (t > 1) t = 1;

            foreach (Material material in deletedCubesMaterials)
                material.SetFloat("Dissolve_Clip", t);

            yield return 0;
        }
    }

    public IEnumerator DropCubes(HashSet<Transform> cubes)
    {
        Dictionary<Transform, Vector3> originCubesPos = new Dictionary<Transform, Vector3>();
        foreach (Transform cube in cubes) originCubesPos.Add(cube, cube.localPosition);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * dropSpeed * Mathf.Pow((t + 1.0f), 2);
            if (t > 1) t = 1;

            foreach (var cube in originCubesPos)
                cube.Key.localPosition = Vector3.Lerp(cube.Value, cube.Value + Vector3.down, t);

            yield return 0;
        }
    }

    public void AddCut(Cuts cut, float value)
    {
        if (cut == Cuts.xMin)
        {
            currentCutXMin += value;
            if (currentCutXMin < defaultCutXMin) currentCutXMin = defaultCutXMin;
            else if (currentCutXMin > defaultCutXMax) currentCutXMin = defaultCutXMax;
            else SoundsManager.PlaySound("Cut");
        }
        else if (cut == Cuts.xMax)
        {
            currentCutXMax += value;
            if (currentCutXMax > defaultCutXMax) currentCutXMax = defaultCutXMax;
            else if (currentCutXMax < defaultCutXMin) currentCutXMax = defaultCutXMin;
            else SoundsManager.PlaySound("Cut");
        }
        else if (cut == Cuts.zMin)
        {
            currentCutZMin += value;
            if (currentCutZMin < defaultCutZMin) currentCutZMin = defaultCutZMin;
            else if (currentCutZMin > defaultCutZMax) currentCutZMin = defaultCutZMax;
            else SoundsManager.PlaySound("Cut");
        }
        else
        {
            currentCutZMax += value;
            if (currentCutZMax > defaultCutZMax) currentCutZMax = defaultCutZMax;
            else if (currentCutZMax < defaultCutZMin) currentCutZMax = defaultCutZMin;
            else SoundsManager.PlaySound("Cut");
        }
    }

    public void ClearAllCuts(bool needSoundEffect = true)
    {
        currentCutXMin = defaultCutXMin;
        currentCutXMax = defaultCutXMax;
        currentCutZMin = defaultCutZMin;
        currentCutZMax = defaultCutZMax;
        if (needSoundEffect) SoundsManager.PlaySound("Cut");
    }

    private IEnumerator CutAnimation()
    {
        while (true)
        {
            float cutXMin = materialCutHint.GetFloat("Cut_X_Min");
            if (cutXMin != currentCutXMin)
            {
                if (Mathf.Abs(cutXMin - currentCutXMin) <= 0.05f) cutXMin = currentCutXMin;
                else cutXMin = Mathf.Lerp(cutXMin, currentCutXMin, Time.deltaTime * cutSpeed);

                foreach (Material materialPiece in materialsPiece)
                    materialPiece.SetFloat("Cut_X_Min", cutXMin);
                materialCutHint.SetFloat("Cut_X_Min", cutXMin);
                materialUnreachableHint.SetFloat("Cut_X_Min", cutXMin);
                FieldWallAnimation.SetFloatForAll("Cut_X_Min", cutXMin);
            }

            float cutXMax = materialCutHint.GetFloat("Cut_X_Max");
            if (cutXMax != currentCutXMax)
            {
                if (Mathf.Abs(cutXMax - currentCutXMax) <= 0.05f) cutXMax = currentCutXMax;
                else cutXMax = Mathf.Lerp(cutXMax, currentCutXMax, Time.deltaTime * cutSpeed);

                foreach (Material materialPiece in materialsPiece)
                    materialPiece.SetFloat("Cut_X_Max", cutXMax);
                materialCutHint.SetFloat("Cut_X_Max", cutXMax);
                materialUnreachableHint.SetFloat("Cut_X_Max", cutXMax);
                FieldWallAnimation.SetFloatForAll("Cut_X_Max", cutXMax);
            }

            float cutZMin = materialCutHint.GetFloat("Cut_Z_Min");
            if (cutZMin != currentCutZMin)
            {
                if (Mathf.Abs(cutZMin - currentCutZMin) <= 0.05f) cutZMin = currentCutZMin;
                else cutZMin = Mathf.Lerp(cutZMin, currentCutZMin, Time.deltaTime * cutSpeed);

                foreach (Material materialPiece in materialsPiece)
                    materialPiece.SetFloat("Cut_Z_Min", cutZMin);
                materialCutHint.SetFloat("Cut_Z_Min", cutZMin);
                materialUnreachableHint.SetFloat("Cut_Z_Min", cutZMin);
                FieldWallAnimation.SetFloatForAll("Cut_Z_Min", cutZMin);
            }

            float cutZMax = materialCutHint.GetFloat("Cut_Z_Max");
            if (cutZMax != currentCutZMax)
            {
                if (Mathf.Abs(cutZMax - currentCutZMax) <= 0.05f) cutZMax = currentCutZMax;
                else cutZMax = Mathf.Lerp(cutZMax, currentCutZMax, Time.deltaTime * cutSpeed);

                foreach (Material materialPiece in materialsPiece)
                    materialPiece.SetFloat("Cut_Z_Max", cutZMax);
                materialCutHint.SetFloat("Cut_Z_Max", cutZMax);
                materialUnreachableHint.SetFloat("Cut_Z_Max", cutZMax);
                FieldWallAnimation.SetFloatForAll("Cut_Z_Max", cutZMax);
            }

            if (cutXMin == defaultCutXMin && cutXMax == defaultCutXMax && cutZMin == defaultCutZMin && cutZMax == defaultCutZMax)
            {
                if (cutActive)
                {
                    cutActive = false;
                    foreach (Material materialPiece in materialsPiece)
                        materialPiece.SetInt("Cut_Active", 0);
                }
            }
            else if (!cutActive)
            {
                cutActive = true;
                foreach (Material materialPiece in materialsPiece)
                    materialPiece.SetInt("Cut_Active", 1);
            }

            yield return 0;
        }
    }
}
