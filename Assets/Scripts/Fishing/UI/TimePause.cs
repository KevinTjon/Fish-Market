using System.Collections;
using UnityEngine;

public class TimePause : MonoBehaviour
{
    private bool isPaused;
    public bool IsPaused { get => isPaused; }

    // Pauses game
    // Slows down simulation gradually when activated
    public static IEnumerator PauseSimulation(float pauseTime)
    {
        float elapsedTime = 0;
        while (elapsedTime < pauseTime)
        {
            Time.timeScale = Mathf.Lerp(1, 0, elapsedTime / pauseTime);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 0;
    }

    public static IEnumerator UnpauseSimulation(float pauseTime)
    {
        float elapsedTime = 0;
        while (elapsedTime < pauseTime)
        {
            Time.timeScale = Mathf.Lerp(0, 1, elapsedTime / pauseTime);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 1;
    }
}