using UnityEngine;
using Oculus.Interaction;

#region #my_code
public class WireTip : MonoBehaviour
{
    [Header("Configuração do Cabo")]
    public string outputPortId; // ID da porta de saída associada a este cabo (ex: 'A', 'B', 'C')

    [Header("Referências Visuais")]
    // transform do ponto de início do cabo, usado para manter o cabo alinhado
    public Transform startPortAnchor; 
    // transform do corpo do fio que será esticado entre o ponto de início e a ponta do cabo
    public Transform corpoDoFio; 

    [Header("Dependências")]
    public WirePuzzle wirePuzzle; // referência ao script do puzzle dos fios

    [Header("Física de Retorno")]
    // velocidade com que o cabo retorna para a posição inicial quando solto longe de uma porta
    public float velocidadeRetorno = 8.0f; 

    private Grabbable grabbable; // referência ao componente de agarrar
    private Vector3 posicaoInicial; // posição inicial do cabo
    private Quaternion rotacaoInicial; // rotação inicial do cabo
    
    private InputPort portaSnapAtual; // referência à porta de entrada onde o cabo está atualmente
    private InputPort portaSobreposta; // referência à porta de entrada que o cabo está atualmente sobrepondo
    private bool isSnapped = false; // indica se o cabo está atualmente encaixado em uma porta de entrada

    /// <summary>
    /// Awake é chamado quando o script é carregado, antes do primeiro frame de atualização
    /// </summary>
    void Awake()
    {
        // obtém a referência ao componente Grabbable para detectar quando o cabo é agarrado
        grabbable = GetComponent<Grabbable>();
        // guarda a posição inicial para poder voltar a ela quando o jogador largar o cabo 
        posicaoInicial = transform.position; 
        // guarda a rotação inicial para manter o cabo sempre na mesma orientação, 
        // evitando deformações estranhas
        rotacaoInicial = transform.rotation; 
        
        if (corpoDoFio != null) // caso exista cabo
        {
            // tira o fio de dentro de gameobjects pais escalados para evitar problemas de scale
            corpoDoFio.SetParent(null); 
        }
    }

    /// <summary>
    /// LateUpdate é chamado uma vez por frame, após todas as atualizações de física e transformações
    /// </summary>
    void LateUpdate() 
    {
        // verifica se o cabo está a ser segurado
        bool isGrabbableActive = grabbable != null && grabbable.SelectingPointsCount > 0; 
        // caso exista um ponto de início definido
        if (startPortAnchor != null)
        {
            // mantém a rotação inicial para evitar que o cabo gire de forma estranha quando agarrado
            transform.rotation = rotacaoInicial; 

            if (isGrabbableActive) // se o cabo estiver a ser segurado
            {
                // calcula a posição local do cabo em relação ao ponto de início para manter o cabo alinhado 
                // com o ponto de início, mesmo quando o jogador se move
                Vector3 posLocal = startPortAnchor.InverseTransformPoint(transform.position); 
                // força o cabo a não se mexer em Z, ou seja, sair de dentro do painel
                posLocal.z = 0f; 
                // aplica a posição local corrigida de volta para o mundo, 
                // garantindo que o cabo siga o ponto de início corretamente
                transform.position = startPortAnchor.TransformPoint(posLocal); 
            }
        }

        // caso exista um cabo e um ponto de início definido
        if (corpoDoFio != null && startPortAnchor != null) 
        {
            // calcula a direção e distância entre o ponto de início e a ponta do cabo para posicionar 
            // e escalar o corpo do fio corretamente
            Vector3 direcao = transform.position - startPortAnchor.position; 
            // calcula a distância para definir o comprimento do fio
            float distancia = direcao.magnitude; 

            // posiciona o corpo do fio no meio entre o ponto de início e a ponta do cabo 
            // para que ele se estique corretamente entre os dois pontos
            corpoDoFio.position = startPortAnchor.position + (direcao / 2f); 

            // se a distância for maior que um pequeno valor para evitar problemas de divisão por zero
            if (distancia > 0.001f) 
            {
                // orienta o corpo do fio para que ele aponte na direção correta entre o 
                // ponto de início e a ponta do cabo, garantindo que o fio se estique na direção certa
                corpoDoFio.up = direcao; 
            }

            // define a grossura do fio com base na escala do cabo para manter uma 
            // aparência consistente, mesmo que o cabo seja escalado
            float grossura = transform.lossyScale.x;  
            // escala o corpo do fio para que ele tenha a grossura correta e o 
            // comprimento baseado na distância entre o ponto de início e a ponta do cabo
            corpoDoFio.localScale = new Vector3(grossura, distancia / 2f, grossura); 
        }

        if (isGrabbableActive) // quando o cabo está a ser segurado
        {
            if (isSnapped) // se o cabo estava encaixado numa InputPort
            {
                isSnapped = false; // marca o cabo como não encaixado
                if (portaSnapAtual != null) // se havia uma porta onde o cabo estava encaixado
                {
                    // avisa o puzzle que o cabo foi desconectado daquela porta
                    wirePuzzle.DisconnectWire(outputPortId); 
                    // desocupa a porta para que outros cabos possam ser encaixados nela
                    portaSnapAtual.isOccupied = false; 
                    // limpa a referência à porta onde o cabo estava encaixado
                    portaSnapAtual = null; 
                }
            }
        }
        else // quando o cabo é largado
        {
            if (!isSnapped) // se o cabo não está encaixado em uma porta de entrada
            {
                // se o cabo está a sobrepôr uma porta de entrada 
                // e essa porta não está ocupada por outro cabo
                if (portaSobreposta != null && !portaSobreposta.isOccupied) 
                {
                    // encaixa o cabo nessa porta, avisando o puzzle que o 
                    // cabo foi conectado naquela porta
                    SnapParaPorta(portaSobreposta); 
                }
                else // se o cabo foi largado longe de uma porta de entrada
                {
                    // move o cabo de volta para a posição inicial de forma suave usando Lerp
                    transform.position = Vector3.Lerp(transform.position, posicaoInicial, Time.deltaTime * velocidadeRetorno); 
                }
            }
        }
    }

    /// <summary>
    /// Método para encaixar o cabo em uma porta de entrada, atualizando o estado do puzzle
    /// </summary>
    /// <param name="portaTarget"></param>
    private void SnapParaPorta(InputPort portaTarget) 
    {
        isSnapped = true; // marca o cabo como encaixado
        portaSnapAtual = portaTarget; // guarda a referência à porta onde o cabo está encaixado
        // marca a porta como ocupada para evitar que outros cabos possam ser encaixados nela
        portaTarget.isOccupied = true; 
        
        // move o cabo para a posição de snap da porta para garantir 
        // que ele se encaixe na porta
        transform.position = portaTarget.snapAnchor.position; 

        if (wirePuzzle != null) // avisa o puzzle que o cabo foi conectado naquela porta
        {
            // passa o ID da porta de saída deste cabo e o ID da porta de entrada 
            // onde ele foi encaixado para o puzzle
            wirePuzzle.ConnectWire(outputPortId, portaTarget.portId); 
        }
    }

    // método chamado quando o cabo começa a sobrepor um collider, 
    // usado para detetar quando o cabo está a sobrepôr uma porta de entrada
    void OnTriggerEnter(Collider other) 
    {
        // obtém a referência ao script da porta de entrada que o cabo está a sobrepôr
        InputPort inputPort = other.GetComponent<InputPort>(); 
        // se o cabo está a sobrepôr uma porta de entrada e essa porta 
        // não está ocupada por outro cabo
        if (inputPort != null && !inputPort.isOccupied) 
        {
            // guarda a referência da porta de entrada que o cabo está a sobrepôr para 
            // que se possa encaixar o cabo se o jogador o largar enquanto estiver 
            // a sobrepôr esse port
            portaSobreposta = inputPort; 
        }
    }

    // método chamado quando o cabo para de sobrepôr um collider
    void OnTriggerExit(Collider other)
    {
        // obtém a referência ao script da porta de entrada que o cabo parou de sobrepôr
        InputPort inputPort = other.GetComponent<InputPort>(); 
        // se a porta de entrada que o cabo parou de sobrepôr é a mesma 
        // que ele tinha guardado como portaSobreposta
        if (inputPort != null && portaSobreposta == inputPort) 
        {
            // limpa a referência à porta de entrada que o cabo estava a sobrepôr,
            // pois o cabo já não está mais sobre ela
            portaSobreposta = null; 
        }
    }
}
#endregion