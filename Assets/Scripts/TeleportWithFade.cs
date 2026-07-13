using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TeleportWithFade : MonoBehaviour
{
    public Transform teleportTarget;            // Target location to teleport to
    public CanvasGroup fadeCanvasGroup;         // UI CanvasGroup for fade effect
    public float fadeDuration = 1f;             // Duration of fade in/out
    public float waitDuration = 1f;             // wait
    public string playerTag = "Player";         // Tag to identify the player
    public string npcTag = "NPC";
    public SceneLoader sceneLoader;
    public bool isSceneLoader = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) /*|| other.CompareTag(npcTag) */)
        {
            if (isSceneLoader) 
            {
                sceneLoader.LoadSceneWithFade();
            } else 
            {
                Debug.Log("teleporting...");
                StartCoroutine(TeleportWithFadeRoutine(other.gameObject));
            }
        }
    }

    private IEnumerator TeleportWithFadeRoutine(GameObject player)
    {
        yield return StartCoroutine(Fade(1));

        // Disable interfering components
        var cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        var movement = player.GetComponent<PlayerController>(); // replace with actual
        if (movement) movement.enabled = false;

        Debug.Log("Teleporting to: " + teleportTarget.position);
        player.transform.position = teleportTarget.position;
        player.transform.rotation = teleportTarget.rotation;

        if (cc) cc.enabled = true;
        if (movement) movement.enabled = true;

        yield return new WaitForSeconds(waitDuration);
        yield return StartCoroutine(Fade(0));
    }


    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
