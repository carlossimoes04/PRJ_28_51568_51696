using UnityEngine;
using UnityEngine.UI;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Classe que representa o painel de scan da sala
/// 
/// Permite ao utilizador verificar se a sala já foi scaneada, abrir o Space Setup do Meta Quest 
/// para fazer o scan da sala, iniciar o jogo (se a sala estiver scaneada) ou voltar ao menu principal
/// </summary>
public class RoomScanPanel : MonoBehaviour
{
    [Header("Imagens de Estado (Com os Textos Embutidos)")]
    [Tooltip("Imagem/Painel que diz que a sala já foi scaneada")]
    [SerializeField] private GameObject imagemSalaScaneada;

    [Tooltip("Imagem/Painel que diz que a sala não foi scaneada")]
    [SerializeField] private GameObject imagemSalaNaoScaneada;

    [Header("Referências de UI - Botões")]
    [Tooltip("Botão para abrir as definições de scan da sala no sistema Meta")]
    [SerializeField] private Button scanButton;

    [Tooltip("Botão para iniciar o jogo - só ativo quando a sala está scanned")]
    [SerializeField] private Button startGameButton;

    [Tooltip("Botão para voltar ao menu principal")]
    [SerializeField] private Button backButton;

    [Header("Referência ao Menu Principal")]
    [Tooltip("Script do menu principal para chamar ShowMainMenu() ao voltar")]
    [SerializeField] private MainMenu mainMenu;

    [Header("Painel de Contexto")]
    [Tooltip("Painel de contexto do jogo que aparece antes de iniciar")]
    [SerializeField] private GameObject contextPanel;

    /// <summary>
    /// Chamado quando o painel de scan da sala é ativado
    /// </summary>
    private void OnEnable()
    {
        CheckRoomScan();
    }

    /// <summary>
    /// Chamado quando o painel de scan da sala é iniciado
    /// </summary>
    private void Start()
    {
        // regista os listeners nos botões
        scanButton.onClick.AddListener(OnScanClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    /// <summary>
    /// Chamado quando o painel de scan da sala é destruído
    /// </summary>
    private void OnDestroy()
    {
        // remove os listeners
        scanButton.onClick.RemoveListener(OnScanClicked);
        startGameButton.onClick.RemoveListener(OnStartGameClicked);
        backButton.onClick.RemoveListener(OnBackClicked);
    }

    /// <summary>
    /// Verifica se a sala está scanned e atualiza a UI consoante o resultado
    /// </summary>
    public void CheckRoomScan()
    {
        bool roomIsScanned = IsRoomScanned(); // flag para saber se a sala está scaneada

        if (roomIsScanned)
        {
            // sala scanned, mostra o texto de confirmação e o botão de iniciar
            if (imagemSalaScaneada != null) imagemSalaScaneada.SetActive(true);
            if (imagemSalaNaoScaneada != null) imagemSalaNaoScaneada.SetActive(false);
            scanButton.gameObject.SetActive(false);
            startGameButton.gameObject.SetActive(true);
        } else
        {
            // sala não scanned, mostra o texto de aviso e o botão de scan
            if (imagemSalaScaneada != null) imagemSalaScaneada.SetActive(false);
            if (imagemSalaNaoScaneada != null) imagemSalaNaoScaneada.SetActive(true);
            scanButton.gameObject.SetActive(true);
            startGameButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Verifica se a sala já foi scaneada pelo utilizador
    /// </summary>
    /// <returns></returns>
    private bool IsRoomScanned()
    {
#if UNITY_EDITOR
        return true;
#else // caso não seja o editor, verifica se a MRUK tem uma sala válida carregada
        if (MRUK.Instance == null)
        {
            return false;
        }
        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        return currentRoom != null;
#endif
    }

    /// <summary>
    /// Abre o Space Setup do Meta Quest para o utilizador poder realizar o scan da sala
    /// 
    /// Após testes, percebeu-se que isto é inútil porque os óculos pedem automaticamente para o
    /// utilizador fazer o scan da sala quando o jogo tenta aceder a uma sala que ainda não foi scaneada
    /// 
    /// No entanto, deixou-se este método por segurança
    /// </summary>
    private async void OnScanClicked()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // limpa a cena atual para evitar conflitos com o Space Setup
            if (MRUK.Instance != null)
            {
                MRUK.Instance.ClearScene();
            }
            // inicia o Space Setup do Meta Quest
            bool success = await OVRScene.RequestSpaceSetup();
            if (success) // se o utilizador completou o Space Setup com sucesso
            {
                if (MRUK.Instance != null) // se a MRUK estiver inicializada, carrega a sala do dispositivo
                {
                    await MRUK.Instance.LoadSceneFromDevice();
                }
            }
            else // se o utilizador cancelou ou ocorreu um erro
            {
                Debug.LogWarning("[RoomScanPanel] o utilizador cancelou o Space Setup ou ocorreu um erro");
            }
            CheckRoomScan(); // atualiza a UI consoante o estado da sala após o Space Setup
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RoomScanPanel] não foi possível iniciar o Space Setup: " + e.Message);
        }
#else
        CheckRoomScan(); // no editor, apenas atualiza a UI sem tentar abrir o Space Setup
        await System.Threading.Tasks.Task.CompletedTask; // evita warning de método async sem await
#endif
    }

    /// <summary>
    /// Inicia o jogo, abrindo o painel de contexto antes de ir para a cena do jogo
    /// </summary>
    private void OnStartGameClicked()
    {
        // abre o ContextPanel em vez de ir direto para a GameScene
        gameObject.SetActive(false);

        if (contextPanel != null) // se o painel de contexto estiver atribuído, mostra-o
        {
            contextPanel.SetActive(true);
        } else
        {
            // se não houver painel de contexto, vai direto para a cena do jogo
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene"); 
        }
    }

    /// <summary>
    /// Volta ao menu principal
    /// </summary>
    private void OnBackClicked()
    {
        if (mainMenu != null)
        {
            mainMenu.ShowMainMenu();
        } else
        {
            gameObject.SetActive(false);
        }
    }
}