using UnityEngine;
using TMPro;
using NavKeypad;
using UnityEngine.Events;

/// <summary>
/// Classe que representa o temporizador de sala
/// </summary>
public class RoomTimer : MonoBehaviour
{
    [Header("Configurações do Tempo")]
    [Tooltip("Tempo limite em minutos")]
    public float startingMinutes = 15f;
    
    [Header("Referências")]
    public TextMeshPro timerText; // referência ao componente de texto que exibe o tempo restante
    public Keypad mainKeypad; // referência ao keypad da porta

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource; // referência ao componente de áudio para tocar sons do temporizador
    [SerializeField] private AudioClip tickClip; // som do ticking do timer
    [SerializeField] private AudioClip buzzerClip; // som do alarme quando o tempo acaba

    [Header("Eventos")]
    public UnityEvent onTimeUp = new UnityEvent(); // evento que é disparado quando o tempo acaba
    
    private float timeRemaining; // tempo restante em segundos
    private bool isTimerRunning = true; // indica se o temporizador está ativo

    /// <summary>
    /// Start é chamado antes da primeira frame de atualização
    /// </summary>
    void Start()
    {
        // inicializa o tempo restante com base nos minutos iniciais
        timeRemaining = startingMinutes * 60f; 
        
        // tenta encontrar o keypad automaticamente
        if (mainKeypad == null)
        {
            mainKeypad = FindAnyObjectByType<Keypad>();
        }

        /* se o keypad foi encontrado, adiciona o método StopTimer 
        ao evento OnAccessGranted do keypad

        ou seja, quando o jogador acertar o código no keypad, 
        o temporizador é parado
        */
        if (mainKeypad != null)
        {
            mainKeypad.OnAccessGranted.AddListener(StopTimer);
        }

        // inicia o som de ticking do temporizador
        if (audioSource != null && tickClip != null)
        {
            audioSource.clip = tickClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Update é chamado uma vez por frame
    /// </summary>
    void Update()
    {
        // se o temporizador não está ativo, não faz nada
        if (!isTimerRunning) return;

        // se o tempo restante é maior que zero
        if (timeRemaining > 0)
        {
            // decrementa o tempo restante com base no tempo do frame
            timeRemaining -= Time.deltaTime;
            // atualiza o display do temporizador
            UpdateTimerDisplay(timeRemaining);
            // muda a cor do texto para amarelo quando faltarem 5 minutos ou menos
            if (timeRemaining <= 300f && timerText != null)
            {
                timerText.color = Color.yellow;
            }
        }
        else // se o tempo acabou
        {
            timeRemaining = 0; // garantir que o tempo não fique negativo
            isTimerRunning = false; // parar o temporizador
            UpdateTimerDisplay(timeRemaining); // atualizar o display para mostrar 00:00
            
            // feedback visual de derrota
            if (timerText != null) timerText.color = Color.red; 
            
            OnTimeUp(); // chama o método que trata o fim do tempo
        }
    }

    /// <summary>
    /// Atualiza o display do temporizador com o tempo 
    /// restante formatado em minutos e segundos
    /// </summary>
    /// <param name="timeToDisplay">Tempo a ser exibido</param>
    void UpdateTimerDisplay(float timeToDisplay)
    {
        // calcula os minutos e segundos restantes
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        if (timerText != null) // se o componente de texto existe, atualiza o texto
        {
            // formata o tempo como MM:SS
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    /// <summary>
    /// Método chamado quando o jogador vence o puzzle
    /// 
    /// O temporizador é parado, o texto muda para verde 
    /// e o som de ticking é interrompido
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false; // parar o temporizador
        
        if (timerText != null) // se o componente de texto existe, muda a cor para verde
            timerText.color = Color.green;
        
        // parar o som de tick-tock se o jogador vencer
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Método chamado quando o tempo do temporizador acaba
    /// </summary>
    void OnTimeUp()
    {
        // parar o som de ticking e tocar o som de alarme
        if (audioSource != null)
        {
            audioSource.Stop();
            if (buzzerClip != null)
            {
                audioSource.clip = buzzerClip;
                audioSource.loop = false;
                audioSource.Play();
            }
        }

        // dispara o evento onTimeUp para notificar outros scripts
        onTimeUp?.Invoke();
    }
}