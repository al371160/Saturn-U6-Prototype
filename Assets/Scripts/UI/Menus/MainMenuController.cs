using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Root panel stack for the Main Menu scene.
/// Keeps Start Demo on its existing SceneLoader; adds Multiplayer and Quit.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject multiplayerPanel;
    [SerializeField] private GameObject matchPrepPanel;
    [SerializeField] private GameObject partyLobbyPanel;
    [SerializeField] private GameObject deployPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        MatchSessionState.EnsureExists();

        if (multiplayerButton != null)
        {
            multiplayerButton.onClick.RemoveListener(ShowMultiplayer);
            multiplayerButton.onClick.AddListener(ShowMultiplayer);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void Start()
    {
        ShowMain();
    }

    public void ShowMain()
    {
        SetPanel(mainPanel, true);
        SetPanel(multiplayerPanel, false);
        SetPanel(matchPrepPanel, false);
        SetPanel(partyLobbyPanel, false);
        SetPanel(deployPanel, false);
    }

    public void ShowMultiplayer()
    {
        SetPanel(mainPanel, false);
        SetPanel(multiplayerPanel, true);
        SetPanel(matchPrepPanel, false);
        SetPanel(partyLobbyPanel, false);
        SetPanel(deployPanel, false);
    }

    public void ShowMatchPrep()
    {
        SetPanel(mainPanel, false);
        SetPanel(multiplayerPanel, false);
        SetPanel(matchPrepPanel, true);
        SetPanel(partyLobbyPanel, false);
        SetPanel(deployPanel, false);
    }

    public void ShowPartyLobby()
    {
        SetPanel(mainPanel, false);
        SetPanel(multiplayerPanel, false);
        SetPanel(matchPrepPanel, false);
        SetPanel(partyLobbyPanel, true);
        SetPanel(deployPanel, false);
    }

    public void ShowDeploy()
    {
        SetPanel(mainPanel, false);
        SetPanel(multiplayerPanel, false);
        SetPanel(matchPrepPanel, false);
        SetPanel(partyLobbyPanel, false);
        SetPanel(deployPanel, true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
