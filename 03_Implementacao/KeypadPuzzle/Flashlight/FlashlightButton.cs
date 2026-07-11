// FlashlightButton.cs
using UnityEngine;
using Oculus.Interaction;

#region #my_code
public class FlashlightButton : MonoBehaviour
{
    [Tooltip("Referência à luz da lanterna")]
    [SerializeField] private Light flashlight;

    // obtém o componente PokeInteractable que está no mesmo GameObject
    private PokeInteractable pokeInteractable;

    void Awake()
    {
        // obtém o componente PokeInteractable que está no objeto
        pokeInteractable = GetComponent<PokeInteractable>();
    }

    void Start()
    {
        // garante que a luz é forçada a desligar assim que o jogo começa
        // if (flashlight != null)
        // {
        //     flashlight.enabled = false;
        // }
    }

    void OnEnable()
    {
        if (pokeInteractable != null)
        {
            pokeInteractable.WhenStateChanged += HandleStateChanged;
        }
    }

    void OnDisable()
    {
        if (pokeInteractable != null)
        {
            pokeInteractable.WhenStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(InteractableStateChangeArgs args)
    {
        // o estado "Select" significa que o botão foi empurrado até ao fim do limite
        if (args.NewState == InteractableState.Select)
        {
            if (flashlight != null)
            {
                // inverte o estado da luz:
                // - se estava ligada, desliga
                // - se estava desligada, liga
                flashlight.enabled = !flashlight.enabled;
            }
        }
    }
}
#endregion