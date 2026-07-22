using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Full-screen deploy interstitial before loading the combat scene.
/// </summary>
public class DeployScreenController : MonoBehaviour
{
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float holdDuration = 1.25f;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private string fallbackSceneName = "Level 1";

    private Coroutine deployRoutine;

    public void BeginDeploy()
    {
        gameObject.SetActive(true);

        if (statusLabel != null)
            statusLabel.text = "Deploying...";

        if (deployRoutine != null)
            StopCoroutine(deployRoutine);
        deployRoutine = StartCoroutine(DeployRoutine());
    }

    private IEnumerator DeployRoutine()
    {
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        if (fadeCanvasGroup != null && fadeDuration > 0f)
            yield return FadeTo(1f);

        MatchSessionState session = MatchSessionState.EnsureExists();
        string sceneName = session != null && !string.IsNullOrEmpty(session.CombatSceneName)
            ? session.CombatSceneName
            : fallbackSceneName;

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float start = fadeCanvasGroup.alpha;
        float timer = 0f;
        fadeCanvasGroup.blocksRaycasts = true;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
