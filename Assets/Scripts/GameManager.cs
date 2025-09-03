using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private bool isAlive = true;
    private bool isPaused = false;
    //private int coins = 0;
    

    private void OnEnable()
    {
        Actions.OnPlayerDeath += ActivateGameOver;
        //Actions.ScorePoint += CollectCoins;
    }

    private void OnDisable()
    {
        Actions.OnPlayerDeath -= ActivateGameOver;
        //Actions.ScorePoint -= CollectCoins;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isAlive)
            PauseGame();

        /*if (!isAlive && Input.anyKeyDown)
            RespawnPlayer();*/
    }

    private void PauseGame()
    {
        if (isPaused)
        {
            Time.timeScale = 1;
            isPaused = false;
            Actions.PauseGame();
            StartCoroutine(TryCloseScene("PauseScene"));
            StartCoroutine(TryCloseScene("OptionsScene"));
            //textPause.SetActive(false);
            //fundoPause.SetActive(false);
        }
        else
        {
            Time.timeScale = 0;
            isPaused = true;
            Actions.PauseGame();
            SceneManager.LoadScene("PauseScene", LoadSceneMode.Additive);
            //textPause.SetActive(true);
            //fundoPause.SetActive(true);
        }
    }

    private IEnumerator TryCloseScene(string scene)
    {
        SceneManager.UnloadSceneAsync(scene);

        yield return null;
    }

    /*private void RespawnPlayer()
    {
        textGO.SetActive(false);
        fundoGO.SetActive(false);
        isAlive = true;
        Actions.RespawnPlayer();
    }*/

    private void ActivateGameOver()
    {
        SceneManager.LoadScene("GameOverScene", LoadSceneMode.Additive);

        //textGO.SetActive(true);
        //fundoGO.SetActive(true);
        //isAlive = false;
    }

    /*private void CollectCoins()
    {
        coins++;
        Actions.ChangeCoins(coins);
    }*/
}