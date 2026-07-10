using UnityEngine;
using Oculus.Interaction;

namespace NavKeypad
{
    public class KeypadButton : MonoBehaviour
    {
        [Header("Value")]
        [SerializeField] private string value; // valor associado a este botão do keypad da porta
        [Header("Component References")]
        [SerializeField] private Keypad keypad; // referência ao script do keypad da porta que este botão pertence

        private PokeInteractable pokeInteractable; // referência ao componente PokeInteractable deste botão

        private bool moving; // flag para indicar se o botão está em movimento (pressionado ou solto)

        /// <summary>
        /// Inicializa o componente PokeInteractable
        /// </summary>
        private void Awake()
        {
            pokeInteractable = GetComponent<PokeInteractable>();
        }

        /// <summary>
        /// Configura o botão para avisar este script sempre 
        /// que for tocado (quando o objeto fica ativo)
        /// </summary>
        private void OnEnable()
        {
            if (pokeInteractable != null)
            {
                pokeInteractable.WhenStateChanged += HandleStateChanged;
            }
        }

        /// <summary>
        /// Desliga o aviso do botão quando o objeto é desativado
        /// 
        /// Evita que o jogo gaste memória ou dê erros com botões que 
        /// já não estão ativos no cenário
        /// </summary>
        private void OnDisable()
        {
            if (pokeInteractable != null)
            {
                pokeInteractable.WhenStateChanged -= HandleStateChanged;
            }
        }

        /// <summary>
        /// Deteta toques físicos/virtuais no botão
        /// 
        /// Se detetar que o botão foi totalmente pressionado, executa a ação de "poke"
        /// </summary>
        /// <param name="args">Informações sobre a mudança de estado do botão</param>
        private void HandleStateChanged(InteractableStateChangeArgs args)
        {
            if (args.NewState == InteractableState.Select)
            {
                PressButton();
            }
        }

        /// <summary>
        /// Executa o poke do botão, enviando o seu valor (número) para o teclado principal
        /// 
        /// Só permite enviar o valor se o botão não se estiver a mexer
        /// (por exemplo, a meio de uma animação)
        /// </summary>
        public void PressButton()
        {
            if (!moving)
            {
                keypad.AddInput(value);
            }
        }
    }
}