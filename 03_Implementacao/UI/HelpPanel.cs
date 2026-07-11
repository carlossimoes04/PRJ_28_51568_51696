using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

#region #my_code
/// <summary>
/// Painel de ajuda do jogo
/// 
/// Mostra vídeos de demonstração das interações possíveis no jogo (Pinch, Grab, Poke, Snap)
/// e permite voltar ao menu principal
/// </summary>
public class HelpPanel : MonoBehaviour
{
    [Header("Referência ao Menu Principal")]
    [SerializeField] private MainMenu mainMenu;

    [Header("Botão de Voltar")]
    [SerializeField] private Button backButton;

    [Header("Video Players")]
    [SerializeField] private VideoPlayer videoPlayer1; // Pinch
    [SerializeField] private VideoPlayer videoPlayer2; // Grab
    [SerializeField] private VideoPlayer videoPlayer3; // Poke
    [SerializeField] private VideoPlayer videoPlayer4; // Snap

    [Header("Botões de Seleção")]
    [SerializeField] private Button btnPinch;
    [SerializeField] private Button btnGrab;
    [SerializeField] private Button btnPoke;
    [SerializeField] private Button btnSnap;

    /// <summary>
    /// Regista os listeners nos botões e ativa os respetivos vídeos 
    /// quando o painel fica visível
    /// </summary>
    private void Start()
    {
        // regista o listener no botão de voltar
        backButton.onClick.AddListener(OnBackClicked);

        // ao clicar nos botões, ativa o respetivo VideoPlayer
        btnPinch.onClick.AddListener(() => SelectVideo(1, btnPinch));
        btnGrab.onClick.AddListener(() => SelectVideo(2, btnGrab));
        btnPoke.onClick.AddListener(() => SelectVideo(3, btnPoke));
        btnSnap.onClick.AddListener(() => SelectVideo(4, btnSnap));
    }

    /// <summary>
    /// Remove os listeners dos botões para evitar memory leaks
    /// e para quando o painel é fechado
    /// </summary>
    private void OnDestroy()
    {
        // remove o listener
        backButton.onClick.RemoveListener(OnBackClicked);

        // remove os listeners dos botões de seleção
        btnPinch.onClick.RemoveAllListeners();
        btnGrab.onClick.RemoveAllListeners();
        btnPoke.onClick.RemoveAllListeners();
        btnSnap.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Quando o painel fica visível, seleciona o vídeo de Pinch por defeito
    /// e ativa o respetivo botão
    /// </summary>
    private void OnEnable()
    {
        SelectVideo(1, btnPinch);
    }

    /// <summary>
    /// Quando o painel é fechado, interrompe todos os vídeos
    /// </summary>
    private void OnDisable()
    {
        StopAllVideos();
    }

    /// <summary>
    /// Seleciona o vídeo correspondente ao índice fornecido 
    /// e ativa o respetivo botão
    /// </summary>
    /// <param name="index">Índice do vídeo a selecionar</param>
    /// <param name="selectedButton">Botão correspondente ao vídeo</param>
    private void SelectVideo(int index, Button selectedButton)
    {
        StopAllVideos(); // interrompe todos os vídeos antes de ativar o selecionado
        ResetButtonColors(); // faz reset da cor de todos os botões para o estado normal
        HighlightButton(selectedButton); // destaca o botão selecionado

        // desativa todos os VideoPlayers antes de ativar o selecionado
        SetVideoActive(videoPlayer1, false);
        SetVideoActive(videoPlayer2, false);
        SetVideoActive(videoPlayer3, false);
        SetVideoActive(videoPlayer4, false);
        
        if (index == 1 && videoPlayer1 != null) // se o índice for 1, ativa o vídeo de Pinch
        {
            SetVideoActive(videoPlayer1, true);
            videoPlayer1.Play();
        }
        else if (index == 2 && videoPlayer2 != null) // se o índice for 2, ativa o vídeo de Grab
        {
            SetVideoActive(videoPlayer2, true);
            videoPlayer2.Play();
        }
        else if (index == 3 && videoPlayer3 != null) // se o índice for 3, ativa o vídeo de Poke
        {
            SetVideoActive(videoPlayer3, true);
            videoPlayer3.Play();
        }
        else if (index == 4 && videoPlayer4 != null) // se o índice for 4, ativa o vídeo de Snap
        {
            SetVideoActive(videoPlayer4, true);
            videoPlayer4.Play();
        }
    }

    /// <summary>
    /// Ativa ou desativa o GameObject do VideoPlayer fornecido
    /// e também o seu GameObject pai, caso exista
    /// </summary>
    /// <param name="player">VideoPlayer a ativar/desativar</param>
    /// <param name="active">True para ativar, False para desativar</param>
    private void SetVideoActive(VideoPlayer player, bool active)
    {
        // se o VideoPlayer não for nulo e o seu GameObject também não for nulo
        if (player != null && player.gameObject != null)
        {
            // ativa ou desativa o GameObject do VideoPlayer
            player.gameObject.SetActive(active);
            // se o VideoPlayer tiver um GameObject pai, também ativa ou desativa o pai
            if (player.transform.parent != null)
            {
                // ativa ou desativa o GameObject pai do VideoPlayer
                player.transform.parent.gameObject.SetActive(active);
            }
        }
    }

    /// <summary>
    /// Interrompe todos os vídeos ativos no painel de ajuda
    /// e também desativa os respetivos GameObjects
    /// </summary>
    private void StopAllVideos()
    {
        if (videoPlayer1 != null) videoPlayer1.Stop();
        if (videoPlayer2 != null) videoPlayer2.Stop();
        if (videoPlayer3 != null) videoPlayer3.Stop();
        if (videoPlayer4 != null) videoPlayer4.Stop();
    }

    /// <summary>
    /// Restaura a cor normal de todos os botões de seleção,
    /// removendo qualquer destaque que possa ter sido aplicado
    /// </summary>
    private void ResetButtonColors()
    {
        Color normalColor = new Color(1f, 1f, 1f, 0.4f); 
        SetButtonColor(btnPinch, normalColor);
        SetButtonColor(btnGrab, normalColor);
        SetButtonColor(btnPoke, normalColor);
        SetButtonColor(btnSnap, normalColor);
    }

    /// <summary>
    /// Destaca o botão fornecido, alterando a sua cor para indicar que está selecionado
    /// </summary>
    /// <param name="button">Botão a destacar</param>
    private void HighlightButton(Button button)
    {
        if (button != null)
        {
            SetButtonColor(button, Color.yellow);
        }
    }

    /// <summary>
    /// Define a cor normal de um botão, alterando o seu ColorBlock
    /// para refletir a cor fornecida
    /// </summary>
    /// <param name="button">Botão a ter a cor definida</param>
    /// <param name="color">Cor a aplicar</param>
    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.selectedColor = color;
        button.colors = cb;
    }

    /// <summary>
    /// Chamado quando o jogador carrega no botão de voltar
    /// 
    /// Volta ao menu principal ou fecha o painel de ajuda 
    /// se não houver referência ao MainMenu
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
#endregion