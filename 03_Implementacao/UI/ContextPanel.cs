using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#region #my_code
/// <summary>
/// Painel de contexto do jogo
/// 
/// Mostra a história do jogo e permite ao jogador iniciar o jogo carregando no botão
/// </summary>
public class ContextPanel : MonoBehaviour
{
    [Header("Referências de UI")]
    [Tooltip("Botão para iniciar o jogo")]
    [SerializeField] private Button startGameButton;

    [Header("Cena de Jogo")]
    [Tooltip("Nome da cena a carregar quando o jogador carregar no botão")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Áudio de Contexto (Voice Over)")]
    [Tooltip("O AudioSource dedicado para reproduzir a voz do ElevenLabs")]
    [SerializeField] private AudioSource voiceOverSource;

    /// <summary>
    /// Regista o listener no botão de iniciar o jogo
    /// </summary>
    private void Start()
    {
        startGameButton.onClick.AddListener(OnStartGameClicked);
    }

    /// <summary>
    /// Remove o listener do botão de iniciar o jogo para evitar memory leaks
    /// </summary>
    private void OnDestroy()
    {
        startGameButton.onClick.RemoveListener(OnStartGameClicked);
    }

    /// <summary>
    /// Quando o painel fica visível, inicia a reprodução do áudio de contexto 
    /// (voice over) se estiver definido
    /// </summary>
    private void OnEnable()
    {
        if (voiceOverSource != null && voiceOverSource.clip != null)
        {
            voiceOverSource.Play();
        }
    }

    /// <summary>
    /// Quando o painel é fechado, interrompe a reprodução do áudio de contexto 
    /// (voice over) se estiver a tocar
    /// </summary>
    private void OnDisable()
    {
        if (voiceOverSource != null)
        {
            voiceOverSource.Stop();
        }
    }

    /// <summary>
    /// Carrega a cena de jogo quando o botão de iniciar é clicado
    /// </summary>
    private void OnStartGameClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
#endregion