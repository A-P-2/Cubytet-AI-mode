using System.Collections;
using TMPro;
using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerScoreValue;
    [SerializeField] private TextMeshProUGUI agentScoreValue;
    [SerializeField] private TextMeshProUGUI timer1Value;
    [SerializeField] private TextMeshProUGUI timer2Value;
    [SerializeField] private GameObject connection;
    [SerializeField] private Material glassMaterial;
    [SerializeField] private Animator animator;

    [Header("Cut")]
    [SerializeField] private float cutYStart = -1f;
    [SerializeField] private float cutYEnd = 1f;
    [SerializeField] private float cutYSpeed = 0.1f;

    [Header("Dissolve")]
    [SerializeField] private float dissolveStart = -1f;
    [SerializeField] private float dissolveEnd = 1f;
    [SerializeField] private float dissolveSpeed = 0.1f;

    [Header("Error")]
    [SerializeField] private float errorStart = -1f;
    [SerializeField] private float errorEnd = 1f;
    [SerializeField] private float errorSpeed = 0.1f;

    [Header("Win and lose")]
    [ColorUsage(true, true)]
    [SerializeField] private Color winColor;
    [ColorUsage(true, true)]
    [SerializeField] private Color loseColor;
    [SerializeField] private float gameoverSpeed = 0.1f;

    Coroutine cutYGlassEffect = null;
    Coroutine dissolveGlassEffect = null;
    Coroutine errorGlassEffect = null;

    private void Start()
    {
        glassMaterial.SetFloat("_CutY_Line_Position", cutYStart);
        glassMaterial.SetFloat("_Dissolve_Line_Position", cutYStart);
        glassMaterial.SetFloat("_Error_Line_Position", cutYStart);
        glassMaterial.SetFloat("_Gameover_Line_Width", -0.35f);
    }

    public void Hide() => animator.SetTrigger("Hide");

    public void CutYGlassEffect()
    {
        if (cutYGlassEffect != null) StopCoroutine(cutYGlassEffect);
        cutYGlassEffect = StartCoroutine(LineEffectIE("_CutY_Line_Position", cutYStart, cutYEnd, cutYSpeed));
    }

    public void DissolveEffect()
    {
        if (dissolveGlassEffect != null) StopCoroutine(dissolveGlassEffect);
        dissolveGlassEffect = StartCoroutine(LineEffectIE("_Dissolve_Line_Position", dissolveStart, dissolveEnd, dissolveSpeed));
    }

    public void ErrorEffect()
    {
        if (errorGlassEffect != null) StopCoroutine(errorGlassEffect);
        errorGlassEffect = StartCoroutine(LineEffectIE("_Error_Line_Position", errorStart, errorEnd, errorSpeed));
    }

    public void gameoverEffect(bool gameWon)
    {
        glassMaterial.SetColor("_Gameover_Line_Color", (gameWon) ? winColor : loseColor);
        StartCoroutine(GameoverEffectIE(gameoverSpeed));
    }

    private IEnumerator LineEffectIE(string lineName, float start, float end, float speed)
    {
        float t = 0;
        glassMaterial.SetFloat(lineName, start);

        while (t < 1)
        {
            t += Time.deltaTime * speed;
            if (t > 1) t = 1;
            yield return null;

            glassMaterial.SetFloat(lineName, Mathf.Lerp(start, end, t));
        }
    }

    private IEnumerator GameoverEffectIE(float speed)
    {
        float t = 0;
        glassMaterial.SetFloat("_Gameover_Line_Width", -0.35f);

        while (t < 1)
        {
            t += Time.deltaTime * speed;
            if (t > 1) t = 1;
            yield return null;

            glassMaterial.SetFloat("_Gameover_Line_Width", Mathf.Lerp(-0.35f, 1, t));
        }
    }

    public void SetPlayerScore(int value) => playerScoreValue.SetText(value.ToString());
    public void SetAgentScore(int value) => agentScoreValue.SetText(value.ToString());
    public void SetConnection(bool connected) => connection.SetActive(!connected);
    public void SetTimer(float value)
    {
        if (value > 9f) value = 9f;
        else if (value < 0f) value = 0f;

        var timerValue = ((int)value).ToString();
        timer1Value.SetText(timerValue);
        timer2Value.SetText(timerValue);
    }
    public Vector3 GetNicePiecePos(Vector3 constructorPos, Piece piece)
    {
        float xOffset, yOffset, zOffset;
        xOffset = -(float)(piece.MaxX + piece.MinX) / 2;
        yOffset = -(float)(piece.MaxY + piece.MinY) / 2;
        zOffset = -(float)(piece.MaxZ + piece.MinZ) / 2;

        return constructorPos + new Vector3(xOffset, yOffset, zOffset);
    }
}
