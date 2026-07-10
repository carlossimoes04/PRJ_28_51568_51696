using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Classe que adiciona efeitos de áudio 3D a um botão UI
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonAudio3D : MonoBehaviour
{
    [Header("Configurações de Áudio 3D")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    [Header("Opções")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private Button button; // referência ao componente Button

    /// <summary>
    /// Inicializa o componente, garantindo que há um AudioSource e configurando-o para áudio 3D
    /// </summary>
    private void Awake()
    {
        button = GetComponent<Button>(); // obtém a referência ao componente Button

        // se não houver um AudioSource atribuído, tenta obter 
        // um do GameObject ou adiciona um novo
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        ConfigureAudioSource3D(); // configura o AudioSource para áudio 3D
    }

    /// <summary>
    /// Regista o listener do botão para tocar o som quando clicado
    /// </summary>
    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    /// <summary>
    /// Remove o listener do botão quando o componente é desativado
    /// </summary>
    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }

    /// <summary>
    /// Configura o AudioSource para reproduzir áudio 3D com as definições apropriadas
    /// </summary>
    private void ConfigureAudioSource3D()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
        audioSource.spatialize = true;
        audioSource.spatializePostEffects = true;
        
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 0.2f;
        audioSource.maxDistance = 5.0f;
    }

    /// <summary>
    /// Reproduz o som de clique do botão usando o AudioSource 3D
    /// </summary>
    public void PlaySound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, volume);
        }
    }
}