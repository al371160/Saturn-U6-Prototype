using UnityEngine;
using UnityEngine.UI;

public class QuickTimeMinigame : MonoBehaviour
{
    [Header("References")]
    public RectTransform cursor;
    public RectTransform safeZone;
    public Slider progressBar;

    [Header("Settings")]
    public float initialCursorSpeed = 100f;
    public float speedIncreasePerHit = 20f;
    public float safeZoneAngle = 60f;
    public float progressPerHit = 0.2f;
    public float progressLossPerMiss = 0.1f;
    public float decayRate = 0.05f;

    [Header("Audio")]
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioSource audioSource;

    public System.Action OnMinigameComplete;
    public System.Action OnMinigameFail;

    private float currentCursorSpeed;
    private float currentCursorAngle = 0f;
    private float safeZoneStartAngle;
    private bool isActive = false;

    public PlayerController player;

    private void Start()
    {
        //player = FindFirstObjectByType<PlayerController>();
    }

    public void StartMinigame()
    {
        currentCursorSpeed = initialCursorSpeed;
        currentCursorAngle = 0f;
        progressBar.value = 0.3f;

        safeZoneStartAngle = Random.Range(0f, 360f);
        safeZone.localEulerAngles = new Vector3(0f, 0f, -safeZoneStartAngle);

        isActive = true;
        player.canMove = false;
    }

    private void Update()
    {
        if (!isActive) return;

        RotateCursor();
        DecayProgress();

        if (Input.GetKeyDown(KeyCode.E))
        {
            CheckIfInSafeZone();
            currentCursorSpeed = -currentCursorSpeed;
        }

        if (progressBar.value <= 0f)
        {
            FailGame();
        }
    }

    private void RotateCursor()
    {
        currentCursorAngle += currentCursorSpeed * Time.deltaTime;
        if (currentCursorAngle >= 360f) currentCursorAngle -= 360f;
        if (currentCursorAngle <= -360f) currentCursorAngle += 360f;

        cursor.localEulerAngles = new Vector3(0f, 0f, -currentCursorAngle);
    }

    private void DecayProgress()
    {
        progressBar.value -= decayRate * Time.deltaTime;
        progressBar.value = Mathf.Clamp01(progressBar.value);
    }

    private void CheckIfInSafeZone()
    {
        float cursorAngle = currentCursorAngle % 360f;
        if (cursorAngle < 0f) cursorAngle += 360f;

        float safeZoneEndAngle = (safeZoneStartAngle + safeZoneAngle) % 360f;

        if (IsAngleWithinRange(cursorAngle, safeZoneStartAngle, safeZoneEndAngle))
        {
            progressBar.value += progressPerHit;
            if (successSound != null) audioSource.PlayOneShot(successSound);

            currentCursorSpeed += (currentCursorSpeed > 0 ? speedIncreasePerHit : -speedIncreasePerHit);
            RandomizeSafeZone();

            if (progressBar.value >= 1f)
            {
                WinGame();
            }
        }
        else
        {
            progressBar.value -= progressLossPerMiss;
            if (failSound != null) audioSource.PlayOneShot(failSound);
        }

        progressBar.value = Mathf.Clamp01(progressBar.value);
    }

    private void RandomizeSafeZone()
    {
        safeZoneStartAngle = Random.Range(0f, 360f);
        safeZone.localEulerAngles = new Vector3(0f, 0f, -safeZoneStartAngle);
    }

    private bool IsAngleWithinRange(float angle, float start, float end)
    {
        if (start < end)
            return angle >= start && angle <= end;
        else
            return angle >= start || angle <= end;
    }

    private void WinGame()
    {
        Debug.Log("Minigame Won!");
        OnMinigameComplete?.Invoke();
        EndMinigame();
    }

    private void FailGame()
    {
        Debug.Log("Minigame Failed!");
        OnMinigameFail?.Invoke();
        EndMinigame();
    }

    private void EndMinigame()
    {
        isActive = false;
        gameObject.SetActive(false);
        currentCursorSpeed = initialCursorSpeed;
        player.canMove = true;
    }
}
