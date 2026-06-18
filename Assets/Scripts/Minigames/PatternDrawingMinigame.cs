using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PatternDrawingMinigame : MonoBehaviour
{
    public int gridSize = 3; // Adjustable grid size
    public GameObject dotPrefab;
    public Transform gridParent;
    public Text instructionText;
    public Image completionBar;
    public Text timerText;
    public float maxTime = 30f;

    private Dictionary<Vector2Int, GameObject> gridDots = new Dictionary<Vector2Int, GameObject>();
    private List<Vector2Int> correctPattern = new List<Vector2Int>();
    private List<Vector2Int> playerInput = new List<Vector2Int>();
    private float timeRemaining;
    private bool gameActive = false;
    public GameObject objectToActivate; // Assign in Inspector
    public GameObject objectToDeactivate; // Assign in Inspector

    void Start()
    {
        GenerateGrid();
        GeneratePattern();
        timeRemaining = maxTime;
        gameActive = true;
    }

    void Update()
    {
        if (gameActive)
        {
            HandleInput();
            UpdateTimer();
        }
    }

    void GenerateGrid()
    {
        gridDots.Clear();
        float spacing = 100f; // Adjust for spacing between dots

        // Create a list of unique letters
        List<char> availableLetters = new List<char>();
        for (char c = 'A'; c < 'A' + (gridSize * gridSize); c++)
        {
            availableLetters.Add(c);
        }

        // Shuffle the list for randomness
        for (int i = 0; i < availableLetters.Count; i++)
        {
            int randIndex = Random.Range(i, availableLetters.Count);
            (availableLetters[i], availableLetters[randIndex]) = (availableLetters[randIndex], availableLetters[i]);
        }

        int letterIndex = 0;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject dot = Instantiate(dotPrefab, gridParent);
                dot.transform.localPosition = new Vector3(x * spacing, y * spacing, 0);
                gridDots[new Vector2Int(x, y)] = dot;

                Text dotText = dot.GetComponentInChildren<Text>();
                dotText.text = availableLetters[letterIndex].ToString();
                letterIndex++;
            }
        }
    }


    void GeneratePattern()
    {
        correctPattern.Clear();
        instructionText.text = "";

        // Start from a random position
        Vector2Int start = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize));
        correctPattern.Add(start);
        instructionText.text += gridDots[start].GetComponentInChildren<Text>().text;

        for (int i = 1; i < gridSize; i++)
        {
            Vector2Int lastPos = correctPattern[i - 1];
            List<Vector2Int> possibleMoves = new List<Vector2Int>();

            // Check all 8 possible adjacent positions
            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                Vector2Int newPos = lastPos + dir;
                if (gridDots.ContainsKey(newPos) && !correctPattern.Contains(newPos))
                {
                    possibleMoves.Add(newPos);
                }
            }

            if (possibleMoves.Count > 0)
            {
                Vector2Int nextPos = possibleMoves[Random.Range(0, possibleMoves.Count)];
                correctPattern.Add(nextPos);
                instructionText.text += gridDots[nextPos].GetComponentInChildren<Text>().text;
            }
            else
            {
                break; // No more moves available, but this should rarely happen
            }
        }
    }


    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int clickedPos = GetClickedDot();
            if (clickedPos != new Vector2Int(-1, -1) && !playerInput.Contains(clickedPos))
            {
                playerInput.Add(clickedPos);
                CheckPattern();
            }
        }
    }

    Vector2Int GetClickedDot()
    {
        if (gridDots == null || gridDots.Count == 0)
        {
            Debug.LogError("gridDots dictionary is empty!");
            return new Vector2Int(-1, -1);
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridParent as RectTransform, // The parent UI element containing the dots
            Input.mousePosition,         // Mouse position
            null,                         // Use null since it's a Screen Space - Overlay Canvas
            out localPoint                // Converted local position
        );

        foreach (var dot in gridDots)
        {
            RectTransform dotRect = dot.Value.GetComponent<RectTransform>();
            if (dotRect == null) continue;


            // Check if the click is inside the dot's area
            if (RectTransformUtility.RectangleContainsScreenPoint(dotRect, Input.mousePosition))
            {
                Debug.Log("hit!");
                return dot.Key;
            }
        }

        return new Vector2Int(-1, -1);
    }


void CheckPattern()
{
    if (playerInput.Count > correctPattern.Count)
    {
        return; // Prevents extra clicks from affecting the game.
    }

    Vector2Int lastInput = playerInput[playerInput.Count - 1];
    Vector2Int correctNextPos = correctPattern[playerInput.Count - 1];

    string lastInputLetter = gridDots[lastInput].GetComponentInChildren<Text>().text;
    string correctNextLetter = gridDots[correctNextPos].GetComponentInChildren<Text>().text;

    if (lastInputLetter != correctNextLetter)
    {
        Debug.Log(":(");
        instructionText.text = "Press 'Restart.'";
        playerInput.Clear(); // Reset only if a completely wrong letter is chosen.
        return;
    }

    if (playerInput.Count == correctPattern.Count) // Check if all inputs are correct
    {
        Debug.Log("win");
        completionBar.fillAmount = Mathf.Clamp(completionBar.fillAmount + 0.5f, 0f, 1f);
        objectToActivate.SetActive(true);
        objectToDeactivate.SetActive(false);
    }
}


    void UpdateTimer()
    {
        timeRemaining -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.Ceil(timeRemaining);
        if (timeRemaining <= 0)
        {
            gameActive = false;
            instructionText.text = "Game Over!";
        }
    }

    void ResetMinigame()
    {
        playerInput.Clear();
        GeneratePattern();
    }
}
