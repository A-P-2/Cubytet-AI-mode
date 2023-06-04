using UnityEngine;

public class BGAnimationAIMode : FieldBGAnimation
{
    [Header("BG Win Colors")]
    [ColorUsage(true, true)]
    [SerializeField] protected Color gameOverWinColor;
    [ColorUsage(true, true)]
    [SerializeField] protected Color wallsGameOverWinColor;

    private void OnEnable()
    {
        materialBG.SetFloat("Line_Softness", 0.001f);
        materialBG.SetFloat("Line_Width", 0);
        materialBG.SetFloat("Line_Pos", -100);

        wallMinX.ChangeColor(wallsOriginalColor, 0.0f);
        wallMaxX.ChangeColor(wallsOriginalColor, 0.0f);
        wallMinZ.ChangeColor(wallsOriginalColor, 0.0f);
        wallMaxZ.ChangeColor(wallsOriginalColor, 0.0f);
        ceiling.ChangeColor(wallsOriginalColor, 0.0f);
    }

    public new void BGAnimationGameOver()
    {
        gameOver = true;

        StopAllCoroutines();

        StartCoroutine(LineWideningAnimation(gameOverColor, gameOverPos, gameOverSoftness, gameOverSoftnessSpeed, gameOverWidth, gameOverWidthSpeed));
        wallMinX.ChangeColor(wallsGameOverColor, wallsGameOverColorChangeSpeed);
        wallMaxX.ChangeColor(wallsGameOverColor, wallsGameOverColorChangeSpeed);
        wallMinZ.ChangeColor(wallsGameOverColor, wallsGameOverColorChangeSpeed);
        wallMaxZ.ChangeColor(wallsGameOverColor, wallsGameOverColorChangeSpeed);
        ceiling.ChangeColor(wallsGameOverColor, wallsGameOverColorChangeSpeed);
    }

    public void BGAnimationGameOverWin()
    {
        gameOver = true;

        StopAllCoroutines();

        StartCoroutine(LineWideningAnimation(gameOverWinColor, gameOverPos, gameOverSoftness, gameOverSoftnessSpeed, gameOverWidth, gameOverWidthSpeed));
        wallMinX.ChangeColor(wallsGameOverWinColor, wallsGameOverColorChangeSpeed);
        wallMaxX.ChangeColor(wallsGameOverWinColor, wallsGameOverColorChangeSpeed);
        wallMinZ.ChangeColor(wallsGameOverWinColor, wallsGameOverColorChangeSpeed);
        wallMaxZ.ChangeColor(wallsGameOverWinColor, wallsGameOverColorChangeSpeed);
        ceiling.ChangeColor(wallsGameOverWinColor, wallsGameOverColorChangeSpeed);
    }
}
