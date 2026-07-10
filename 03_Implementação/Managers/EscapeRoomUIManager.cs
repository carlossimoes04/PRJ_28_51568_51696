using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Gere a ativação, desativação e o posicionamento dos painéis de interface 
/// de vitória e derrota em frente ao jogador
/// </summary>
public class EscapeRoomUIManager : MonoBehaviour
{
    [Header("Painéis")]
    [Tooltip("Referência para o painel que mostra a mensagem de vitória.")]
    [SerializeField] private VictoryPanel victoryPanel;
    [Tooltip("Referência para o painel que mostra a mensagem de derrota.")]
    [SerializeField] private DefeatPanel defeatPanel;
    [Tooltip("Referência para o painel de fundo escuro que se ativa com a vitória ou derrota.")]
    [SerializeField] private GameObject backPanel;
    [Header("Configurações do Canvas")]
    [Tooltip("Distância em metros a que o painel será posicionado à frente do jogador.")]
    [SerializeField] private float distanceFromPlayer = 2.0f;
    [Tooltip("Referência para o canvas que contém os painéis de vitória e derrota.")]
    [SerializeField] private Canvas canvas;

    /// <summary>
    /// Método chamado no momento em que o script é carregado
    /// 
    /// Garante que os painéis começam desativados
    /// </summary>
    private void Awake()
    {
        if (canvas != null) 
            // desativa o canvas para que os painéis não sejam visíveis no início
            canvas.gameObject.SetActive(false);
        // verifica se a referência do painel de vitória existe
        if (victoryPanel != null) 
            // desativa o objeto do painel de vitória
            victoryPanel.gameObject.SetActive(false);
        // verifica se a referência do painel de derrota existe
        if (defeatPanel != null) 
            // desativa o objeto do painel de derrota
            defeatPanel.gameObject.SetActive(false);
        // verifica se a referência do painel de trás existe
        if (backPanel != null) 
            // desativa o objeto do painel de trás
            backPanel.SetActive(false);
    }

    /// <summary>
    /// Método chamado antes da primeira atualização de frame do jogo
    /// 
    /// Inicia a pesquisa da porta e do temporizador
    /// </summary>
    private void Start()
    {
        // como a porta e o temporizador são instanciados em runtime pelo VirtualDoorSpawner,
        // inicia-se uma corrotina para procurá-los e registar os listeners automaticamente
        StartCoroutine(FindDoorAndTimerRegisterListeners());
    }

    /// <summary>
    /// Corrotina que procura repetidamente na cena a porta e o temporizador para registar 
    /// as funções de resposta aos eventos
    /// 
    /// Esta função foi implementada porque a porta virtual (DoorOpenDelayController) e o 
    /// temporizador da sala (RoomTimer) são instanciados dinamicamente na cena em tempo de 
    /// execução (runtime), e por isso não foi possível a partir do inspetor do Unity
    /// atribuir os listeners aos eventos OnDoorOpenedEvent e onTimeUp
    /// 
    /// A corrotina permite aguardar de forma assíncrona até que as instâncias fiquem disponíveis 
    /// na memória sem bloquear o processamento principal (frame rate) do jogo
    /// </summary>
    /// <returns>Retorna um IEnumerator que permite ao Unity gerir a execução desta corrotina 
    /// ao longo dos frames</returns>
    private IEnumerator FindDoorAndTimerRegisterListeners()
    {
        // variável para armazenar a referência ao controlador da porta
        DoorOpenDelayController doorController = null;

        // variável para armazenar a referência ao temporizador da sala
        RoomTimer timer = null;

        // procura os componentes repetidamente até serem instanciados
        while (doorController == null || timer == null)
        {
            if (doorController == null) doorController = FindAnyObjectByType<DoorOpenDelayController>();
            if (timer == null) timer = FindAnyObjectByType<RoomTimer>();
            yield return new WaitForSeconds(0.5f);
        }

        // regista os métodos aos eventos dinamicamente em runtime
        doorController.OnDoorOpenedEvent.AddListener(ShowVictory);
        timer.onTimeUp.AddListener(ShowDefeat);
    }

    /// <summary>
    /// Ativa a interface de vitória e o fundo, colocando-os em frente ao jogador
    /// </summary>
    public void ShowVictory()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);
        if (victoryPanel != null) victoryPanel.gameObject.SetActive(true);
        if (backPanel != null) backPanel.SetActive(true);
        PositionPanelInFrontOfPlayer();
    }

    /// <summary>
    /// Ativa a interface de derrota e o fundo, colocando-os em frente ao jogador
    /// </summary>
    public void ShowDefeat()
    {
        if (canvas != null) canvas.gameObject.SetActive(true);
        if (defeatPanel != null) defeatPanel.gameObject.SetActive(true);
        if (backPanel != null) backPanel.SetActive(true);
        PositionPanelInFrontOfPlayer();
    }

    /// <summary>
    /// Posiciona e orienta o canvas da interface diretamente em frente aos olhos do jogador no espaço 3D
    /// </summary>
    private void PositionPanelInFrontOfPlayer()
    {
        if (canvas == null) return; // se o canvas não estiver atribuído, sai da função

        // obter a câmara do jogador
        Transform playerCam = Camera.main != null ? Camera.main.transform : null;
        if (playerCam != null) // se a câmara do jogador for válida
        {
            Vector3 forwardXZ = playerCam.forward; // direção frontal da câmara do jogador
            forwardXZ.y = 0; // y = 0 para evitar que o painel fique inclinado para cima ou para baixo
            
            // se a direção frontal for nula, usa o vetor vertical da câmara
            if (forwardXZ == Vector3.zero) forwardXZ = playerCam.up;

            // normaliza o vetor de direção para garantir que tem comprimento unitário
            // normalizar é importante para que a distância do painel seja consistente, 
            // independentemente da direção da câmara
            forwardXZ.Normalize();

            // calcula e atribui a nova posição do painel somando o offset de distância 
            // à posição da câmara do jogador
            canvas.transform.position = playerCam.position + (forwardXZ * distanceFromPlayer);
            
            // calcula a direção do painel apontado para a câmara do jogador
            Vector3 lookDirection = canvas.transform.position - playerCam.position;

            // anula a componente y para manter o painel sempre na horizontal, 
            // evitando que ele fique inclinado para cima ou para baixo
            lookDirection.y = 0;

            // se a direção de olhar não for nula, ajusta a rotação do painel para
            // que ele fique de frente para a câmara do jogador
            if (lookDirection != Vector3.zero)
            {
                canvas.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        else
        {
            Debug.LogWarning("[EscapeRoomUIManager] Câmara principal (Camera.main) não encontrada para posicionar os painéis!");
        }
    }
}