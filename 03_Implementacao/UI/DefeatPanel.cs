using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#region #my_code
/// <summary>
/// Painel de derrota do jogo
/// 
/// Mostra a mensagem de derrota e permite voltar ao menu principal
/// </summary>
public class DefeatPanel : MonoBehaviour
{
    [Header("Referências de UI")]
    [Tooltip("Botão para voltar ao menu principal")]
    [SerializeField] private Button backButton;

    [Header("Cena do Menu Principal")]
    [Tooltip("Nome da cena do menu principal")]
    [SerializeField] private string mainMenuScene = "MainMenuScene";

    /// <summary>
    /// Regista o listener no botão de voltar
    /// </summary>
    private void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
    }

    /// <summary>
    /// Remove o listener do botão de voltar para evitar memory leaks
    /// </summary>
    private void OnDestroy()
    {
        backButton.onClick.RemoveListener(OnBackClicked);
    }

    /// <summary>
    /// Volta ao menu principal carregando a cena correspondente
    /// </summary>
    private void OnBackClicked()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}
#endregion