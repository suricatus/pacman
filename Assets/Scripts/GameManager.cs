using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Zenject;
using Cysharp.Threading.Tasks;
using Tools.PrizeManager.Models;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private RankingManager _rankingManager;
    private ScoreBasedPrizeEvaluator _prizeEvaluator;

    [Header("Refs")]
    [SerializeField] private Ghost[] ghosts;
    [SerializeField] private Pacman pacman;
    [SerializeField] private Transform pellets;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Text winnerText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text livesText;
    [SerializeField] private int ptsAbsoluteSize = 20;

    [Header("Sounds")]
    [SerializeField] private SoundEvent musicStart;
    [SerializeField] private SoundEvent sfxGameOver;
    [SerializeField] private SoundEvent sfxWinner;

    [Header("Vitória")]
    [SerializeField] private bool goToVictoryOnClear = true;
    [SerializeField] private string victorySceneName = "Victory";
    [SerializeField] private Sprite victoryPrizeSprite;
    [SerializeField] private float winDelay = 2.5f;
    [SerializeField] private bool scaleGhostlessScore = true;

    [Header("Derrota")]
    [SerializeField] private bool goToVictoryOnGameOver = true;
    [SerializeField] private Sprite gameOverPrizeSprite;
    [SerializeField] private float loseDelay = 2.5f;

    [Header("Regras especiais de prêmio")]
    [Tooltip("Score equivalente a 100% da fase sem comer fantasmas (ex.: 2460).")]
    [SerializeField] private int pelletPerfectScore = 2460;
    [Tooltip("Score equivalente a 100% da fase (O valor que você considera cheio para a vitória: ex.2770).")]
    [SerializeField] private int allPelletsMaxScore = 2770;
    [Tooltip("ItemId no RewardConfig do Labubu (top prêmio apenas no 100% perfeito).")]
    [SerializeField] private string labubuItemId = "bola";
    [Tooltip("ItemId no RewardConfig da Garrafinha (forçado em derrota com score > pelletPerfectScore).")]
    [SerializeField] private string garrafinhaItemId = "copo";

    // Estado do jogo
    public int Score { get; private set; } = 0;
    public int Lives { get; private set; } = 1;

    private int _ghostMultiplier = 1;
    private int _totalPelletCount = -1;
    private bool _isEnding;
    private bool _ateAlgumFantasma = false;
    private bool _noHasPellet = false;
    private bool _noHasOtherPrizes = false; // indica que só resta o top prize (labubu)

    private const string ZERO_PRIZE_ID = "caneta 1";

    [Inject]
    public void Construct(RankingManager rankingManager, ScoreBasedPrizeEvaluator prizeEvaluator)
    {
        _rankingManager = rankingManager;
        _prizeEvaluator = prizeEvaluator;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }

        // PrizeManager's top prize is configured in ScoreBasedPrizeEvaluator inspector
    }

    private void Start()
    {
        NewGame();
        if (AudioManager.I != null)
        {
            AudioManager.I.SetMute(false);
            if (musicStart) AudioManager.I.PlayMusic(musicStart, loop: false, fade: 0.2f);
        }
    }

    private void NewGame()
    {
        _isEnding = false;
        SetScore(0);
        SetLives(1);
        _ateAlgumFantasma = false;
        NewRound();
    }

    private void NewRound()
    {
        if (gameOverText) gameOverText.enabled = false;
        if (winnerText) winnerText.enabled = false;

        foreach (Transform pellet in pellets)
            pellet.gameObject.SetActive(true);

        _ateAlgumFantasma = false;
        _totalPelletCount = -1;
        ResetState();
    }

    private void ResetState()
    {
        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i].ResetState();
        }
        pacman.ResetState();
    }

    private void StartTransitionToVictory(string message, Sprite prizeSprite, float delay)
    {
        if (_isEnding) return;
        _isEnding = true;

        Time.timeScale = 0f;

        for (int i = 0; i < ghosts.Length; i++)
            if (ghosts[i] != null) ghosts[i].gameObject.SetActive(false);
        if (pacman != null) pacman.gameObject.SetActive(false);

        VictoryPayload.Message = message;
        VictoryPayload.PrizeSprite = prizeSprite;

        StartCoroutine(LoadSceneAfterDelay(victorySceneName, delay));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void HandleWin()
    {
        if (winnerText) winnerText.enabled = true;

        if (AudioManager.I != null)
        {
            AudioManager.I.Play2D(sfxWinner);
            AudioManager.I.StopMusic(0.2f);
        }

        if (goToVictoryOnClear)
        {
            PrepareVictoryPayloadAsync(true).Forget();
        }
    }

    private void SendResultsToGoogleSheets()
    {
        var googleSheetsService = FindObjectOfType<GoogleSheetsService>();

        if (googleSheetsService != null)
        {
            var playerName = PlayerPrefs.GetString("PlayerName", "Desconhecido");
            var playerEmail = PlayerPrefs.GetString("PlayerEmail", "");
            var playerPhone = PlayerPrefs.GetString("PlayerCellphone");
            var gameResult = $"Score: {Score}";
            var prizeWon = GetPrizeWon();
            
            googleSheetsService.SendPlayerDataWithResult(
                playerName,
                playerEmail,
                playerPhone,
                gameResult,
                prizeWon,
                success => 
            {
                if (success)
                    Debug.Log("Game results sent to Google Sheets");
                else
                {
                    Debug.LogWarning("Failed to send game results to Google Sheets");
                }
            });
        }
    }

    private string GetPrizeWon()
    {
        if (string.IsNullOrEmpty(VictoryPayload.PrizeItemId))
        {
            return "Nenhum prêmio";
        }

        if (_prizeEvaluator == null)
        {
            return VictoryPayload.PrizeItemId;
        }

        var prize = _prizeEvaluator.GetPrizeInfo(VictoryPayload.PrizeItemId);
        if (prize != null)
        {
            return prize.PrizeName;
        }

        return VictoryPayload.PrizeItemId;
    }

    public void GameOverFromTimer()
    {
        if (gameOverText) gameOverText.enabled = true;

        if (goToVictoryOnGameOver)
        {
            PrepareVictoryPayloadAsync(false).Forget();
        }
    }

    private void GameOverFromDeath()
    {
        if (gameOverText) gameOverText.enabled = true;

        if (AudioManager.I != null)
        {
            if (sfxGameOver) AudioManager.I.StopMusic(0.2f);
        }

        if (goToVictoryOnGameOver)
        {
            PrepareVictoryPayloadAsync(false).Forget();
        }
    }

    private async UniTaskVoid PrepareVictoryPayloadAsync(bool isWin)
    {
        try
        {
            VictoryPayload.Clear();
            VictoryPayload.Score = Score;

            if (Score == 0)
            {
                VictoryPayload.IsZeroPoints = true;
                VictoryPayload.Message = "Obrigado pela participação! \n\n Não foi dessa vez!";

                if (_prizeEvaluator != null)
                {
                    if (_prizeEvaluator.TryForceItem(ZERO_PRIZE_ID, out var rrZero))
                    {
                        var before = _prizeEvaluator.GetRemainingForPrize(ZERO_PRIZE_ID);
                        var result = await _prizeEvaluator.ForceSpecificPrize(ZERO_PRIZE_ID, "zero_points");
                        
                        if (result.Success)
                        {
                            VictoryPayload.PrizeSprite = result.PrizeSprite;
                            VictoryPayload.PrizeItemId = result.PrizeId;
                            
                            var after = _prizeEvaluator.GetRemainingForPrize(ZERO_PRIZE_ID);
                            PrizeTelemetry.LogPrizeGranted(result, before, after, _prizeEvaluator.GetTotalRemaining(), "zero_points");
                        }
                    }
                    else
                    {
                        VictoryPayload.PrizeSprite = null;
                        VictoryPayload.PrizeItemId = null;
                    }
                }
                else
                {
                    VictoryPayload.PrizeSprite = null;
                    VictoryPayload.PrizeItemId = null;
                }

                SendResultsToGoogleSheets();
                SubmitScoreToLeaderboard().Forget();
                StartTransitionToVictory(VictoryPayload.Message, VictoryPayload.PrizeSprite, isWin ? winDelay : loseDelay);
                return;
            }

            VictoryPayload.IsZeroPoints = false;

            if (_prizeEvaluator != null)
            {
                var ptsText = Score.ToString("N0");

                if (isWin && _noHasPellet && _prizeEvaluator.TryForceItem(labubuItemId, out var _))
                {
                    var before = _prizeEvaluator.GetRemainingForPrize(labubuItemId);
                    var result = await _prizeEvaluator.ForceSpecificPrize(labubuItemId, "win");
                    
                    if (result.Success)
                    {
                        VictoryPayload.Message = $"PARABÉNS!\nVocê fez {ptsText} pontos!";
                        VictoryPayload.PrizeSprite = result.PrizeSprite;
                        VictoryPayload.PrizeItemId = result.PrizeId;
                        Score = allPelletsMaxScore;
                        VictoryPayload.Score = Score;

                        var after = _prizeEvaluator.GetRemainingForPrize(labubuItemId);
                        PrizeTelemetry.LogPrizeGranted(result, before, after, _prizeEvaluator.GetTotalRemaining(), "win");
                        
                        SendResultsToGoogleSheets();
                        SubmitScoreToLeaderboard().Forget();
                        StartTransitionToVictory(VictoryPayload.Message, VictoryPayload.PrizeSprite, winDelay);
                        return;
                    }
                }

                if (!isWin && Score > pelletPerfectScore && _prizeEvaluator.TryForceItem(garrafinhaItemId, out var _))
                {
                    var before = _prizeEvaluator.GetRemainingForPrize(garrafinhaItemId);
                    var result = await _prizeEvaluator.ForceSpecificPrize(garrafinhaItemId, "game_over");
                    
                    if (result.Success)
                    {
                        VictoryPayload.Message = $"PARABÉNS!\nVocê fez {ptsText} pontos!";
                        VictoryPayload.PrizeSprite = result.PrizeSprite;
                        VictoryPayload.PrizeItemId = result.PrizeId;
                        VictoryPayload.Score = Score;

                        var after = _prizeEvaluator.GetRemainingForPrize(garrafinhaItemId);
                        PrizeTelemetry.LogPrizeGranted(result, before, after, _prizeEvaluator.GetTotalRemaining(), "game_over");
                        
                        SendResultsToGoogleSheets();
                        SubmitScoreToLeaderboard().Forget();
                        StartTransitionToVictory(VictoryPayload.Message, VictoryPayload.PrizeSprite, loseDelay);
                        return;
                    }
                }

                VictoryPayload.Message = Score == 0 ? 
                    "Obrigado pela participação!" : 
                    $"PARABÉNS!\nVocê fez {ptsText} pontos!";
                VictoryPayload.Score = Score;

                var res = await _prizeEvaluator.EvaluateAndAwardPrize(Score);

                if (res.Success && !string.IsNullOrEmpty(res.PrizeId))
                {
                    VictoryPayload.PrizeSprite = res.PrizeSprite;
                    VictoryPayload.PrizeItemId = res.PrizeId;

                    var evalAfter = _prizeEvaluator.GetRemainingForPrize(res.PrizeId);
                    var evalBefore = evalAfter + 1;
                    PrizeTelemetry.LogPrizeGranted(res, evalBefore, evalAfter, _prizeEvaluator.GetTotalRemaining(), isWin ? "win" : "game_over");
                    
                    SendResultsToGoogleSheets();
                    SubmitScoreToLeaderboard().Forget();
                    StartTransitionToVictory(VictoryPayload.Message, VictoryPayload.PrizeSprite, isWin ? winDelay : loseDelay);
                    return;
                }

                Debug.LogWarning($"[GameManager] EvaluateAndAwardPrize returned no prize. Success={res.Success}, PrizeId={res.PrizeId}, Message={res.Message}");

                bool onlyTopLeft = _prizeEvaluator.CheckOnlyTopPrizeLeft();
                _noHasOtherPrizes = onlyTopLeft;

                if (!isWin && !_noHasPellet && _noHasOtherPrizes)
                {
                    if (_prizeEvaluator.TryForceItem(labubuItemId, out var _))
                    {
                        var before = _prizeEvaluator.GetRemainingForPrize(labubuItemId);
                        var rrFallback = await _prizeEvaluator.ForceSpecificPrize(labubuItemId, "no_other_prizes");
                        
                        if (rrFallback.Success)
                        {
                            VictoryPayload.Message = $"PARABÉNS!\nVocê fez {ptsText} pontos!";
                            VictoryPayload.PrizeSprite = rrFallback.PrizeSprite;
                            VictoryPayload.PrizeItemId = rrFallback.PrizeId;

                            var after = _prizeEvaluator.GetRemainingForPrize(labubuItemId);
                            PrizeTelemetry.LogPrizeGranted(rrFallback, before, after, _prizeEvaluator.GetTotalRemaining(), "no_other_prizes");
                            
                            SendResultsToGoogleSheets();
                            SubmitScoreToLeaderboard().Forget();
                            StartTransitionToVictory(VictoryPayload.Message, VictoryPayload.PrizeSprite, isWin ? winDelay : loseDelay);
                            return;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] PrepareVictoryPayloadAsync exception: {e}");
        }

        var ptsFallback = Score.ToString("N0");
        VictoryPayload.Message = Score <= 0 ? "Obrigado pela participação!" : $"PARABÉNS!\nVocê fez {ptsFallback} pontos!";
        VictoryPayload.PrizeSprite = (Score > 0) ? victoryPrizeSprite : null;
        VictoryPayload.PrizeItemId = null;
        
        SendResultsToGoogleSheets();
        SubmitScoreToLeaderboard().Forget();
        StartTransitionToVictory(VictoryPayload.Message, VictoryPayload.PrizeSprite, isWin ? winDelay : loseDelay);
    }

    public void PacmanEaten()
    {
        if (pacman == null) return;

        pacman.DeathSequence();
        SetLives(Lives - 1);
    }

    public void GhostEaten(Ghost ghost)
    {
        _ateAlgumFantasma = true;
        int points = ghost.points * _ghostMultiplier;
        SetScore(Score + points);
        _ghostMultiplier++;
    }

    public void PelletEaten(Pellet pellet)
    {
        pellet.gameObject.SetActive(false);
        SetScore(Score + pellet.points);

        if (!HasRemainingPellets())
        {
            _noHasPellet = true;

            if (Score < allPelletsMaxScore)
            {
                SetScore(allPelletsMaxScore);
            }
            HandleWin();
        }
    }

    private int CountAllPellets()
    {
        int c = 0;
        foreach (Transform t in pellets)
        {
            if (t.GetComponent<Pellet>() != null || t.GetComponent<PowerPellet>() != null)
                c++;
        }
        return c;
    }

    private int CountRemainingPellets()
    {
        int c = 0;
        foreach (Transform t in pellets)
        {
            if (t.gameObject.activeSelf &&
                (t.GetComponent<Pellet>() != null || t.GetComponent<PowerPellet>() != null))
                c++;
        }
        return c;
    }

    private float GetPelletCompletion01()
    {
        if (_totalPelletCount <= 0) _totalPelletCount = CountAllPellets();
        if (_totalPelletCount <= 0) return 0f;

        int remaining = CountRemainingPellets();
        int collected = _totalPelletCount - remaining;
        return Mathf.Clamp01(collected / (float)_totalPelletCount);
    }

    public void PowerPelletEaten(PowerPellet pellet)
    {
        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i].Frightened.Enable(pellet.duration);
        }

        PelletEaten(pellet);
        CancelInvoke(nameof(ResetGhostMultiplier));
        Invoke(nameof(ResetGhostMultiplier), pellet.duration);
    }

    private void SetLives(int lives)
    {
        this.Lives = lives;
        if (livesText != null) livesText.text = "x" + lives.ToString();
    }

    private void SetScore(int score)
    {
        Score = score;

        if (scoreText != null)
        {
            scoreText.supportRichText = true;
            scoreText.text = $"{score:00} <size={ptsAbsoluteSize}>PTS</size>";
        }
    }

    private bool HasRemainingPellets()
    {
        foreach (Transform pellet in pellets)
        {
            if (pellet.gameObject.activeSelf) return true;
        }
        return false;
    }

    private void ResetGhostMultiplier()
    {
        _ghostMultiplier = 1;
    }

    public void OnPacmanDeathAnimationFinished()
    {
        if (Lives > 0)
        {
            ResetState();
        }
        else
        {
            GameOverFromDeath();
        }
    }

    private async UniTaskVoid SubmitScoreToLeaderboard()
    {
        if (_rankingManager == null)
            return;

        if (!_rankingManager.IsPlayerRegistered())
            return;

        var result = await _rankingManager.SubmitScoreAsync(Score);
    }
}
