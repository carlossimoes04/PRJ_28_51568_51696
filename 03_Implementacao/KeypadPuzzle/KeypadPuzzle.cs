using UnityEngine;
using TMPro;
using Meta.XR.MRUtilityKit;
using System.Collections;

#region #my_code
public class KeypadPuzzle : BasePuzzle
{
    [Header("Lógica do Keypad")]
    [Tooltip("Sequência de teclas que o jogador tem de premir para resolver este puzzle")]
    public int[] correctSequence = { 1, 3, 2 };
    private int currentStep = 0; // passo atual na sequência correta

    private float lastPressTime = 0f; // variável para armazenar o tempo do último toque
    private float debounceDelay = 0.5f; // tempo mínimo entre toques para evitar vários registos do mesmo toque

    [Header("Display")]
    public TextMeshPro displayText; // referência ao componente TextMeshPro que mostra o estado do puzzle

    // variável para armazenar o dígito que será revelado ao jogador quando o puzzle for resolvido
    private int revealedDigit = -1;

    /// <summary>
    /// Configura a rotação e a posição inicial do teclado numérico na parede
    /// 
    /// Chamado pelo PuzzleSpawner no início do jogo para fixar o teclado no local correto
    /// </summary>
    /// <param name="anchor">Âncora da parede onde o teclado será colocado</param>
    public override void Initialize(MRUKAnchor anchor)
    {
        puzzleId = "wall_keypad_01"; // id do puzzle
    }

    /// <summary>
    /// Chamado pelo EscapeRoomManager após atribuir o dígito a este puzzle
    /// 
    /// Guarda o dígito que será revelado quando o jogador resolver o puzzle
    /// </summary>
    public override void SetupCodeDigit(int digit)
    {
        revealedDigit = digit;
        Debug.Log($"[KeypadPuzzle] dígito atribuído é {digit}");
    }

    /// <summary>
    /// Ativa o puzzle do teclado numérico, permitindo que o jogador interaja com ele
    /// </summary>
    public override void Activate()
    {
        currentStep = 0; // reinicia o passo atual na sequência correta
        isSolved = false; // reinicia o estado de resolução do puzzle
        SetDisplay(""); // limpa o display do teclado
    }

    /// <summary>
    /// Chamado quando o jogador pressiona um botão do teclado numérico
    /// </summary>
    /// <param name="buttonNumber">Número do botão pressionado</param>
    public void OnButtonPressed(int buttonNumber)
    {
        if (isSolved) return; // se o puzzle já estiver resolvido, ignora a entrada

        // se o tempo desde o último toque for menor que o atraso de debounce, ignora a entrada
        if (Time.time - lastPressTime < debounceDelay) return;

        // atualiza o tempo do último toque
        lastPressTime = Time.time;

        // verifica se o botão pressionado corresponde ao próximo número na sequência correta
        if (buttonNumber == correctSequence[currentStep])
        {
            currentStep++; // avança para o próximo passo na sequência correta

            // se o jogador completou a sequência correta
            if (currentStep >= correctSequence.Length)
            {
                SetDisplay("Correct Code"); // mostra mensagem de sucesso no display
                CompletePuzzle(); // marca o puzzle como resolvido

                // se um dígito foi atribuído a este puzzle, 
                // mostra o dígito no display após um pequeno atraso
                if (revealedDigit >= 0)
                {
                    StartCoroutine(RevealDigitAfterDelay(3f));
                }
            }
        }
        else // se o botão pressionado não corresponde ao próximo número na sequência correta
        {
            SetDisplay("Invalid Sequence"); // mostra mensagem de erro no display
            currentStep = 0; // reinicia a sequência correta
        }
    }

    /// <summary>
    /// Revela o dígito atribuído ao puzzle no display após um atraso especificado
    /// </summary>
    /// <param name="delay">Atraso antes de revelar o dígito</param>
    private IEnumerator RevealDigitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetDisplay($"Digit: {revealedDigit}");
    }

    /// <summary>
    /// Atualiza o texto do display do teclado numérico
    /// </summary>
    /// <param name="message">Mensagem a ser exibida</param>
    private void SetDisplay(string message)
    {
        if (displayText != null)
            displayText.text = message;
    }
}
#endregion