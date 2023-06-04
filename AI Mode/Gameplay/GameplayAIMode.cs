using System.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameplayAIMode : MonoBehaviour
{
    private enum GameStatus
    {
        difficultySelecting,
        gameOnPause,
        gameOver,
        gameplay
    }

    [SerializeField] private Field playerField;
    [SerializeField] private Field agentField;
    [SerializeField] private Transform constructor;
    [SerializeField] private CameraMovements constrCamera;
    [SerializeField] private GameObject[] pieces;
    [SerializeField] private float gameSpeedTimerEasy = 10.0f;
    [SerializeField] private float gameSpeedTimerHard = 5.0f;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private AgentManager agentManager;

    [Header("UI")]
    [SerializeField] private GameplayUI gameplayUI;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private DifficultyDialog difficultyDialog;
    [SerializeField] private GameoverMenuAIMode gameoverMenu;

    [Header("Animation")]
    [SerializeField] private BGAnimationAIMode bgAnimationPlayer;
    [SerializeField] private BGAnimationAIMode bgAnimationAgent;
    [SerializeField] private FieldAnimationAIMode fieldAnimation;

    private GameObject nextPiece;

    private bool easyMode = true;
    private float gameSpeed;
    private bool skip = false;

    private GameStatus gameStatus = GameStatus.difficultySelecting;

    private int recordScore = -1;
    private int playerScore = 0;
    private int agentScore = 0;
    private uint inGameTime = 0;

    private void Start()
    {
        StartCoroutine(IEStart());
    }

    private void Update()
    {
        if (gameStatus == GameStatus.gameplay) KeyboardInput();
    }

    private IEnumerator IEStart()
    {
        playerInput.ActiveMouse = false;
        playerInput.ActiveKeyboard = false;

        gameplayUI.SetPlayerScore(playerScore);
        gameplayUI.SetAgentScore(agentScore);
        gameplayUI.SetTimer(0f);
        constrCamera.SetRotation(0.0f, 15.0f);

        GameObject currentPiece = pieces[Random.Range(0, pieces.Length)];
        playerField.SpawnPiece(Instantiate(currentPiece));
        agentField.SpawnPiece(Instantiate(currentPiece));

        nextPiece = Instantiate(pieces[Random.Range(0, pieces.Length)]);
        nextPiece.transform.position = gameplayUI.GetNicePiecePos(constructor.position, nextPiece.GetComponent<Piece>());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return StartCoroutine(difficultyDialog.SelectDifficulty());
        easyMode = difficultyDialog.Answer == DifficultyDialog.AnswerType.Easy;

        if (easyMode)
        {
            agentManager.EasyMode = true;
            gameSpeed = gameSpeedTimerEasy;
        }
        else
        {
            agentManager.EasyMode = false;
            gameSpeed = gameSpeedTimerHard;
        }
        if (easyMode) SteamDataManager.GetScoreEasyAIMode(out recordScore);
        else SteamDataManager.GetScoreHardAIMode(out recordScore);
        gameplayUI.SetConnection(recordScore >= 0);
        gameStatus = GameStatus.gameplay;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(InGameTimer());
        StartCoroutine(ConstrCameraSlowRotation());
        StartCoroutine(NextTurn());

        playerInput.ActiveMouse = true;
        playerInput.ActiveKeyboard = true;
        SoundsManager.PlayMusic("FieldMusic");
    }

    private void KeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(PauseGame());
            return;
        }

        skip = Input.GetKeyDown(DataManager.Controls[DataControls.ControlsTypes.speedUp]);
    }

    private IEnumerator PauseGame()
    {
        gameStatus = GameStatus.gameOnPause;
        Time.timeScale = 0;
        playerInput.ActiveMouse = false;
        playerInput.ActiveKeyboard = false;
        skip = false;

        pauseMenu.OpenMenu();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        while(pauseMenu.IsActive) yield return null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerInput.ActiveMouse = true;
        playerInput.ActiveKeyboard = true;
        Time.timeScale = 1;
        gameStatus = GameStatus.gameplay;
    }

    private IEnumerator InGameTimer()
    {
        while(gameStatus != GameStatus.gameOver)
        {
            yield return new WaitForSeconds(1f);
            inGameTime++;
        }
    }

    private IEnumerator ConstrCameraSlowRotation()
    {
        while (gameStatus != GameStatus.gameOver)
        {
            constrCamera.AddRotation(Time.deltaTime * 30.0f, 0.0f);
            yield return 0;
        }
    }

    private IEnumerator NextTurn()
    {
        while (true)
        {
            yield return StartCoroutine(agentManager.CalculateMove(agentField.FieldData, agentField.CurrentPiece, (int)gameSpeed * 2));

            float timer = Time.time;
            float timerEnd = timer + gameSpeed;

            float delayAgentMove = gameSpeed / agentManager.TopBestMoves.Count;
            float timerAgentMove = timer;

            playerInput.ActiveKeyboard = true;
            while (timer < timerEnd)
            {
                if (gameStatus == GameStatus.gameplay)
                {
                    if (skip) break;
                    else
                    {
                        timer += Time.deltaTime;

                        if (timer >= timerAgentMove && agentManager.TopBestMoves.TryDequeue(out var agentMove))
                        {
                            agentField.MovePieceCoord(agentMove.X, agentMove.Z, agentMove.Rotation);
                            timerAgentMove += delayAgentMove;
                        }

                        gameplayUI.SetTimer(timerEnd - timer);
                        yield return null;
                    }
                }
                else yield return null;
            }
            playerInput.ActiveKeyboard = false;
            fieldAnimation.ClearAllCuts(false);

            agentField.MovePieceCoord(agentManager.BestMove.X, agentManager.BestMove.Z, agentManager.BestMove.Rotation);
            gameplayUI.SetTimer(0f);

            gameplayUI.CutYGlassEffect();
            var installAnimPlayer = StartCoroutine(playerField.InstallPiece());
            var installAnimAI = StartCoroutine(agentField.InstallPiece());
            yield return installAnimPlayer;
            yield return installAnimAI;

            if (playerField.GameOver || agentField.GameOver)
            {
                GameOver(false, false, playerField.GameOver, agentField.GameOver);
                break;
            }

            SetPiecesAchievements();

            playerScore += 5;
            agentScore += 5;
            gameplayUI.SetPlayerScore(playerScore);
            gameplayUI.SetAgentScore(agentScore);

            int numberOfFullPlanePlayer, numberOfFullPlaneAgent;
            numberOfFullPlanePlayer = numberOfFullPlaneAgent = 0;

            int fullPlanePlayer, fullPlaneAgent;

            while (((fullPlanePlayer = playerField.GetFullPlane()) != -1) | (fullPlaneAgent = agentField.GetFullPlane()) != -1)
            {
                gameplayUI.DissolveEffect();

                var playerDeleteCoroutine = StartCoroutine(playerField.DeletePlane(fullPlanePlayer));
                var agentDeleteCoroutine = StartCoroutine(agentField.DeletePlane(fullPlaneAgent));
                yield return playerDeleteCoroutine;
                yield return agentDeleteCoroutine;

                if (fullPlanePlayer != -1) numberOfFullPlanePlayer++;
                if (fullPlaneAgent != -1) numberOfFullPlaneAgent++;
            }
            if (numberOfFullPlanePlayer > 0)
            {
                SetPanelsAchievements(numberOfFullPlanePlayer);

                playerScore += numberOfFullPlanePlayer * numberOfFullPlanePlayer * 100;
                gameplayUI.SetPlayerScore(playerScore);
            }
            if (numberOfFullPlaneAgent > 0)
            {
                agentScore += numberOfFullPlaneAgent * numberOfFullPlaneAgent * 100;
                gameplayUI.SetAgentScore(agentScore);
            }

            if (playerScore >= 2500 || agentScore >= 2500)
            {
                GameOver(playerScore >= 2500, agentScore >= 2500, false, false);
                break;
            }

            playerField.SetUnreachableHint();
            agentField.SetUnreachableHint();

            playerField.SpawnPiece(Instantiate(nextPiece));
            agentField.SpawnPiece(Instantiate(nextPiece));
            Destroy(nextPiece);

            nextPiece = Instantiate(pieces[Random.Range(0, pieces.Length)]);
            nextPiece.transform.position = gameplayUI.GetNicePiecePos(constructor.position, nextPiece.GetComponent<Piece>());

            yield return null;
        }
    }

    private void SetPiecesAchievements()
    {
        SteamDataManager.IncreaseStat("PIECES_PLACED", 1);
        SteamDataManager.IncreaseStat("PIECES_PLACED_GLOBAL", 1);
    }

    private void SetPanelsAchievements(int numberOfFullPlane)
    {
        if (numberOfFullPlane >= 1) SteamDataManager.UnlockAchievement("PANELS_1");
        if (numberOfFullPlane >= 2) SteamDataManager.UnlockAchievement("PANELS_2");
        if (numberOfFullPlane >= 3) SteamDataManager.UnlockAchievement("PANELS_3");
        if (numberOfFullPlane >= 4) SteamDataManager.UnlockAchievement("PANELS_4");

        SteamDataManager.IncreaseStat("PANELS_FILLED", numberOfFullPlane);
        SteamDataManager.IncreaseStat("PANELS_FILLED_GLOBAL", numberOfFullPlane);
    }

    private void SetScoreAchievements(int finalResult)
    {
        if (finalResult >= 10000 || recordScore >= 10000)
        {
            SteamDataManager.UnlockAchievement("SCORE_10_AI_EASY");
            if (!easyMode) SteamDataManager.UnlockAchievement("SCORE_10_AI_HARD");
        }
        if (finalResult >= 30000 || recordScore >= 30000)
        {
            SteamDataManager.UnlockAchievement("SCORE_30_AI_EASY");
            if (!easyMode) SteamDataManager.UnlockAchievement("SCORE_30_AI_HARD");
        }
        if (finalResult >= 50000 || recordScore >= 50000)
        {
            SteamDataManager.UnlockAchievement("SCORE_50_AI_EASY");
            if (!easyMode) SteamDataManager.UnlockAchievement("SCORE_50_AI_HARD");
        }
    }

    private void GameOver(bool playerWon, bool agentWon, bool playerLost, bool agentLost)
    {
        SoundsManager.PauseMusic("FieldMusic", 1.4f);

        SteamDataManager.UnlockAchievement("WIN_AI_EASY");
        if (!easyMode) SteamDataManager.UnlockAchievement("WIN_AI_HARD");

        gameStatus = GameStatus.gameOver;
        playerInput.ActiveMouse = false;
        playerInput.ActiveKeyboard = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerWon) bgAnimationPlayer.BGAnimationGameOverWin();
        else if (playerLost) bgAnimationPlayer.BGAnimationGameOver();

        if (agentWon) bgAnimationAgent.BGAnimationGameOverWin();
        else if (agentLost) bgAnimationAgent.BGAnimationGameOver();

        if (playerWon || agentWon) gameplayUI.gameoverEffect(true);
        else if (playerLost || agentLost) gameplayUI.gameoverEffect(false);

        int gameResult = 4;
        if (playerWon && playerScore > agentScore) gameResult = 0;
        else if (agentWon && playerScore < agentScore) gameResult = 1;
        else if ((playerLost && !agentLost) || (playerLost && agentLost && playerScore < agentScore)) gameResult = 2;
        else if ((!playerLost && agentLost) || (playerLost && agentLost && playerScore > agentScore)) gameResult = 3;

        int finalResult = playerScore - agentScore;
        if (gameResult == 0) finalResult += 5000;
        else if (gameResult != 2) finalResult += 2500;

        finalResult = (int)(finalResult * 100 / Unity.Mathematics.math.sqrt(inGameTime));
        if (finalResult < 0) finalResult = 0;

        SetScoreAchievements(finalResult);
        if (inGameTime <= 213 && gameResult == 0) SteamDataManager.UnlockAchievement("TIME_AI");

        gameplayUI.Hide();
        gameoverMenu.Show(gameResult, (easyMode) ? 0 : 1, playerScore, agentScore, inGameTime, finalResult, recordScore);
    }
}
