using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#region #my_code
/// <summary>
/// Classe que representa o painel de vitória do jogo
/// </summary>
public class VictoryPanel : MonoBehaviour
{
    [Header("Referências de UI")]
    [Tooltip("Botão para voltar ao menu principal")]
    [SerializeField] private Button backButton;

    [Header("Cena do Menu Principal")]
    [Tooltip("Nome da cena do menu principal")]
    [SerializeField] private string mainMenuScene = "MainMenuScene";

    /// <summary>
    /// Chamado quando o painel de vitória é ativado
    /// </summary>
    private void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
    }

    /// <summary>
    /// Chamado quando o painel de vitória é destruído
    /// </summary>
    private void OnDestroy()
    {
        backButton.onClick.RemoveListener(OnBackClicked);
    }

    /// <summary>
    /// Chamado quando o botão de voltar é clicado, carregando a cena do menu principal
    /// </summary>
    private void OnBackClicked()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

}
#endregion