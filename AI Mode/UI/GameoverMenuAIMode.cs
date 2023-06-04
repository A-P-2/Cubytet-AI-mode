using System;
using UnityEngine;
using TMPro;

public class GameoverMenuAIMode : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject[] titles;
    [SerializeField] private GameObject[] difficulties;
    [SerializeField] private TextMeshProUGUI playerScore;
    [SerializeField] private TextMeshProUGUI aiScore;
    [SerializeField] private TextMeshProUGUI time;
    [SerializeField] private TextMeshProUGUI finalScore;
    [SerializeField] private TextMeshProUGUI record;
    [SerializeField] private GameObject newRecord;
    [SerializeField] private GameObject steamError;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        canvas.SetActive(false);
        foreach (var title in titles) title.SetActive(false);
        foreach (var difficulty in difficulties) difficulty.SetActive(false);
        newRecord.SetActive(false);
        steamError.SetActive(false);
    }

    public void Show(int gameResult, int difficulty, int playerScore, int aiScore, uint time, int finalScore, int record)
    {
        animator.SetTrigger("Show");
        StartGameoverMusic();

        titles[gameResult].SetActive(true);
        difficulties[difficulty].SetActive(true);

        this.playerScore.SetText(playerScore.ToString());
        this.aiScore.SetText(aiScore.ToString());
        this.time.SetText(TimeSpan.FromSeconds(time).ToString(@"mm\:ss"));
        this.finalScore.SetText(finalScore.ToString());

        if (record >= 0)
        {
            this.record.SetText(record.ToString());
            if (finalScore > record)
            {
                if (difficulty == 0) SteamDataManager.SetScoreEasyAIMode(finalScore);
                else SteamDataManager.SetScoreHardAIMode(finalScore);

                newRecord.SetActive(true);
            }
        }
        else
        {
            this.record.SetText("???");
            steamError.SetActive(true);
        }
    }

    public void PlayAgain() => LevelLoader.LoadLevel(3);
    public void MainMenu() => LevelLoader.LoadLevel(0);

    public void StartGameoverMusic() => SoundsManager.PlayMusic("GameoverMusic");
}
