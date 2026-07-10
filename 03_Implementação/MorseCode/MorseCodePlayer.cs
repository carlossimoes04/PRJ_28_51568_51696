using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MorseCodePlayer : MonoBehaviour
{
    [Header("Configurações do Morse")]
    [Tooltip("A mensagem que a coluna vai transmitir (apenas letras e números)")]
    [SerializeField] private string messageToPlay = "SOS";
    
    [Tooltip("Duração base de um ponto em segundos - o traço será 3x este valor")]
    [SerializeField] private float dotDuration = 0.2f;

    [Tooltip("Volume de reprodução do Beep")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    [Tooltip("Música ao completar o puzzle")]
    [SerializeField] private AudioClip completionMusic;

    // referência ao componente AudioSource para reproduzir o som do código Morse
    private AudioSource audioSource;

    // dicionário que mapeia caracteres para seus equivalentes em código Morse
    private Dictionary<char, string> morseAlphabet = new Dictionary<char, string>()
    {
        {'A', ".-"},   {'B', "-..."}, {'C', "-.-."}, {'D', "-.."},  {'E', "."},
        {'F', "..-."}, {'G', "--."},  {'H', "...."}, {'I', ".."},   {'J', ".---"},
        {'K', "-.-"},  {'L', ".-.."}, {'M', "--"},   {'N', "-."},   {'O', "---"},
        {'P', ".--."}, {'Q', "--.-"}, {'R', ".-."},  {'S', "..."},  {'T', "-"},
        {'U', "..-"},  {'V', "...-"}, {'W', ".--"},  {'X', "-..-"}, {'Y', "-.--"},
        {'Z', "--.."},
        {'1', ".----"}, {'2', "..---"}, {'3', "...--"}, {'4', "....-"}, {'5', "....."},
        {'6', "-...."}, {'7', "--..."}, {'8', "---.."}, {'9', "----."}, {'0', "-----"},
        {' ', " "}
    };

    /// <summary>
    /// Configura e valida o componente de som do Unity no momento em que o objeto é carregado
    /// 
    /// Garante que o AudioSource tem um ficheiro de áudio associado e aplica as definições
    /// necessárias para a reprodução do código Morse
    /// </summary>
    private void Awake()
    {
        // obtém a referência ao componente AudioSource anexado ao GameObject
        audioSource = GetComponent<AudioSource>();
        
        // verifica se existe ficheiro de som no AudioSource
        if (audioSource.clip == null)
        {
            Debug.LogError("[MorseCodePlayer] sem ficheiro no audio source");
        }

        // aplica as configurações ideias para o código Morse diretamente no AudioSource
        audioSource.playOnAwake = false; // não reproduz automaticamente ao iniciar
        audioSource.loop = true; // mantém o loop ativo para o som do Beep
        audioSource.volume = volume; // define o volume de reprodução do Beep
    }

    /// <summary>
    /// Inicia a transmissão do código Morse assim que o objeto é ativado na cena
    /// </summary>
    private void Start()
    {
        // garante que não há corrotinas em execução antes de iniciar a transmissão
        StopAllCoroutines();

        // se o AudioSource e o ficheiro de áudio estiverem configurados corretamente
        if (audioSource != null && audioSource.clip != null)
        {
            // inicia a corrotina que reproduz o código Morse de forma contínua
            StartCoroutine(PlayMorseRoutine());
        }
    }

    /// <summary>
    /// Corrotina que reproduz a mensagem em código Morse de forma contínua
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayMorseRoutine()
    {
        while (true) // loop infinito para repetir a mensagem continuamente
        {
            // converte a mensagem para maiúsculas para garantir 
            // que todos os caracteres estão no formato correto
            string upperMessage = messageToPlay.ToUpper();

            // itera sobre cada caractere da mensagem
            foreach (char character in upperMessage)
            {
                // verifica se o caractere está no dicionário de código Morse
                if (morseAlphabet.TryGetValue(character, out string morseCode))
                {
                    // se o caractere for um espaço
                    if (morseCode == " ")
                    {
                        // intervalo de silêncio entre os pares de letra e número
                        yield return new WaitForSeconds(dotDuration * 4f);
                    }
                    else // se o caractere for uma letra ou número
                    {
                        // itera sobre cada símbolo (ponto ou traço) do código Morse correspondente
                        foreach (char symbol in morseCode)
                        {
                            // determina a duração do som com base no símbolo (ponto ou traço)
                            // ponto = 1 unidade de tempo, traço = 3 unidades de tempo
                            float symbolDuration = (symbol == '.') ? dotDuration : (dotDuration * 3f);
                            // liga o som do Beep
                            audioSource.Play();
                            // espera a duração do símbolo antes de desligar o som
                            yield return new WaitForSeconds(symbolDuration);
                            // desliga o som do Beep
                            audioSource.Stop();
                            // intervalo de silêncio entre os símbolos
                            yield return new WaitForSeconds(dotDuration);
                        }
                        // intervalo de silêncio entre os caracteres (letras ou números)
                        yield return new WaitForSeconds(dotDuration * 2f);
                    }
                }
            }
            // intervalo de silêncio entre as repetições da mensagem
            yield return new WaitForSeconds(5f);
        }
    }

    /// <summary>
    /// Interrompe a transmissão do código Morse e, 
    /// se houver música de conclusão, inicia a sua reprodução em loop
    /// </summary>
    public void PararMorse()
    {
        // garante que não há corrotinas em execução antes de iniciar a transmissão
        StopAllCoroutines();
        // se o AudioSource estiver configurado corretamente
        if (audioSource != null)
        {
            audioSource.Stop(); // interrompe a reprodução do Beep

            // se existir música de conclusão, toca-a em loop
            if (completionMusic != null)
            {
                audioSource.clip = completionMusic; // substitui o ficheiro de áudio do beep pela música
                audioSource.loop = true; // mantém o loop ativo para a música
                audioSource.Play(); // inicia a reprodução da música de conclusão
            }
        }
        Debug.Log($"[MorseCodePlayer] puzzle resolvido");
    }

    /// <summary>
    /// Atualiza a mensagem que será transmitida em código Morse e reinicia a transmissão
    /// </summary>
    public void AtualizarMensagem(string novaMensagem)
    {
        // se a nova mensagem for nula ou vazia, não faz nada
        if (string.IsNullOrEmpty(novaMensagem)) return;
        // atualiza a mensagem que será transmitida
        messageToPlay = novaMensagem;
        // garante que não há corrotinas em execução antes de iniciar a transmissão
        StopAllCoroutines();
        // se existir AudioSource, interrompe a reprodução do Beep
        if (audioSource != null) audioSource.Stop();
        // inicia a corrotina que reproduz o código Morse da nova mensagem
        StartCoroutine(PlayMorseRoutine());
        Debug.Log($"[MorseCodePlayer] mensagem atualizada para {novaMensagem}");
    }
}