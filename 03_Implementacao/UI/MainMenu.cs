using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Classe que representa o menu principal do jogo
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Botões do Menu Principal")]
    [Tooltip("Botão que inicia o fluxo de jogo (abre o painel de scan)")]
    [SerializeField] private Button playButton;

    [Tooltip("Botão que abre o painel de ajuda com as interações possíveis")]
    [SerializeField] private Button helpButton;

    [Tooltip("Botão que fecha a aplicação")]
    [SerializeField] private Button quitButton;

    [Header("Painéis")]
    [Tooltip("Painel que verifica e faz scan da sala antes de iniciar o jogo")]
    [SerializeField] private GameObject roomScanPanel;

    [Tooltip("Painel que mostra as interações disponíveis no jogo")]
    [SerializeField] private GameObject helpPanel;

    [Tooltip("Painel do menu principal (este próprio canvas/panel)")]
    [SerializeField] private GameObject mainMenuPanel;

    /// <summary>
    /// Chamado quando o menu principal é iniciado
    /// </summary>
    private void Start()
    {
        // garante que apenas o menu principal está visível no arranque
        ShowMainMenu();

        // registar listeners nos botões
        playButton.onClick.AddListener(OnPlayClicked);
        helpButton.onClick.AddListener(OnHelpClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    /// <summary>
    /// Chamado quando o menu principal é destruído
    /// </summary>
    private void OnDestroy()
    {
        // remove os listeners nos botões
        playButton.onClick.RemoveListener(OnPlayClicked);
        helpButton.onClick.RemoveListener(OnHelpClicked);
        quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    /// <summary>
    /// Chamado quando o jogador carrega no botão "Play"
    /// </summary>
    private void OnPlayClicked()
    {
        // abre o painel de scan da sala e esconde o menu principal
        mainMenuPanel.SetActive(false);
        roomScanPanel.SetActive(true);

        // verifica o estado da sala e atualiza a UI do painel de scan
        RoomScanPanel scanPanel = roomScanPanel.GetComponent<RoomScanPanel>();
        if (scanPanel != null)
        {
            scanPanel.CheckRoomScan();
        }
    }

    /// <summary>
    /// Chamado quando o jogador carrega no botão "Help"
    /// </summary>
    private void OnHelpClicked()
    {
        mainMenuPanel.SetActive(false);
        helpPanel.SetActive(true);
    }

    /// <summary>
    /// Chamado quando o jogador carrega no botão "Quit"
    /// </summary>
    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Mostra o menu principal e esconde todos os outros painéis
    /// </summary>
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        roomScanPanel.SetActive(false);
        helpPanel.SetActive(false);
    }
}