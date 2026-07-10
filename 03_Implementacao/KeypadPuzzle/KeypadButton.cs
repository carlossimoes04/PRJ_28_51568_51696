using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Componente que representa um botão do keypad colorido
/// </summary>
public class KeypadButton : MonoBehaviour
{
    public KeypadPuzzle puzzle; // referência ao puzzle do keypad colorido
    public int buttonNumber; // número do botão (0-9) que este botão representa

    private PokeInteractable pokeInteractable; // referência ao componente PokeInteractable deste botão

    /// <summary>
    /// Inicializa o componente
    /// 
    /// Obtém a referência ao PokeInteractable
    /// </summary>
    void Awake()
    {
        pokeInteractable = GetComponent<PokeInteractable>();
    }

    /// <summary>
    /// Configura o botão para avisar quando o estado do PokeInteractable mudar
    /// </summary>
    void OnEnable()
    {
        if (pokeInteractable != null)
        {
            pokeInteractable.WhenStateChanged += HandleStateChanged;
        }
    }

    /// <summary>
    /// Remove o evento de mudança de estado do PokeInteractable 
    /// quando o botão é desativado
    /// </summary>
    void OnDisable()
    {
        if (pokeInteractable != null)
        {
            pokeInteractable.WhenStateChanged -= HandleStateChanged;
        }
    }

    /// <summary>
    /// Responde à mudança de estado do botão
    /// 
    /// É acionado automaticamente sempre que o utilizador interage com o botão
    /// 
    /// Se detetar que o botão foi totalmente pressionado (estado Select), avisa o script 
    /// do puzzle indicando o número do botão premido
    /// </summary>
    /// <param name="args">Informações sobre a mudança de estado do botão</param>
    private void HandleStateChanged(InteractableStateChangeArgs args)
    {
        if (args.NewState == InteractableState.Select)
        {
            puzzle.OnButtonPressed(buttonNumber);
        }
    }
}