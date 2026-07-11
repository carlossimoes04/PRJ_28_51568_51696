using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

#region #my_code
public class EscapeRoomManager : MonoBehaviour
{
    [Header("Gestores")]
    public PuzzleSpawner puzzleSpawner; // referência ao script que faz spawn dos puzzles
    public WallAnalyzer wallAnalyzer; // referência ao script que analisa as paredes da sala
    public FloorAnalyzer floorAnalyzer; // referência ao script que analisa o chão da sala
    public VirtualDoorSpawner virtualDoor; // referência ao script que faz spawn da porta virtual

    public int[] finalCode = new int[2]; // código final para abrir a porta da sala

    void Start()
    {
        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneReady); 
        // executa a lógica principal apenas quando a cena estiver pronta, 
        // garantindo que o MRUK já tem a sala carregada e analisada
    }

    void OnSceneReady()
    {
        MRUKRoom room = MRUK.Instance.GetCurrentRoom(); // obtém a sala atual a partir do MRUK
        if (room == null) return; // se não houver sala, não faz nada

        // analisa a sala (paredes e chão))
        if (wallAnalyzer != null)
            wallAnalyzer.Initialize(room);
        
        if (floorAnalyzer != null)
            floorAnalyzer.AnalyzeFloor(room);

        // faz spawn da porta virtual
        if (virtualDoor != null)
        {
            Debug.Log("[EscapeRoomManager] A gerar a Porta Virtual...");
            virtualDoor.SpawnVirtualDoor(room);
        }

        // o código final será gerado após o spawn dos puzzles,
        // para que o tamanho coincida com o número de puzzles
        StartCoroutine(SpawnAfterCameraReady(room));
    }

    /// <summary>
    /// Faz spawn dos puzzles e do NPC (se configurado) apenas quando a câmara do jogador estiver pronta,
    /// para evitar problemas de spawn em posições erradas ou puzzles a aparecerem antes do jogador estar 
    /// preparado para os ver
    /// <br/>
    /// <br/> O código final é gerado e atribuído aos puzzles após o spawn, garantindo que cada puzzle tem um 
    /// dígito único do código
    /// </summary>
    /// <param name="room">Sala atual detetada pelo MRUK</param>
    IEnumerator SpawnAfterCameraReady(MRUKRoom room)
    {
        Camera cam = puzzleSpawner.playerCamera; // referência à câmara do jogador (definida no PuzzleSpawner)

        // espera até a câmara estar a uma altura razoável
        while (cam == null || cam.transform.position.y < 0.5f)
        {
            Debug.Log("[EscapeRoomManager] À espera da câmara... altura atual: " + (cam != null ? cam.transform.position.y : 0f));
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1.5f); // espera um pouco mais para garantir que o jogador está pronto para ver os puzzles a aparecerem

        Debug.Log("[EscapeRoomManager] Câmara pronta! A fazer spawn dos puzzles..."); // debug

        List<BasePuzzle> spawnedPuzzles = puzzleSpawner.InitializePuzzles(room, wallAnalyzer, floorAnalyzer); // faz spawn dos puzzles e guarda as referências

        // gerar o código final com tantos dígitos quantos puzzles existem
        AssignCodeDigitsToPuzzles(spawnedPuzzles);

        // ativa todos os puzzles após o spawn, para garantir que estão visíveis e prontos para interagir
        foreach (var puzzle in spawnedPuzzles)
        {
            if (puzzle != null)
            {
                puzzle.gameObject.SetActive(true);
                puzzle.Activate();
                Debug.Log($"[EscapeRoomManager] Puzzle '{puzzle.puzzleId}' ativado.");
            }
        }
    }

    /// <summary>
    /// Gera o finalCode com N dígitos (um por puzzle) e atribui
    /// aleatoriamente cada posição do código a cada puzzle
    /// </summary>
    /// <param name="puzzles">Lista de puzzles para atribuir os dígitos do código</param>
    private void AssignCodeDigitsToPuzzles(List<BasePuzzle> puzzles)
    {
        if (puzzles == null || puzzles.Count == 0) // se não houver puzzles, não faz nada
        {
            Debug.LogWarning("[EscapeRoom] Nenhum puzzle para atribuir dígitos!"); // debug
            return;
        }

        // ajustar o tamanho do código ao número de puzzles
        finalCode = new int[puzzles.Count]; // o código terá tantos dígitos quanto puzzles existirem
        for (int i = 0; i < finalCode.Length; i++) // gerar um dígito aleatório para cada posição do código
        {
            finalCode[i] = Random.Range(0, 10); // 0 a 9
        }

        Debug.Log($"[EscapeRoom] Código gerado ({finalCode.Length} dígitos): {string.Join("", finalCode)}"); // debug do código gerado

        // criar lista de posições [0, 1, 2, ...] e baralhar
        List<int> positions = Enumerable.Range(0, puzzles.Count).ToList(); 

        // baralha a lista de posições usando o algoritmo de Fisher-Yates
        // baseado em: https://www.geeksforgeeks.org/dsa/shuffle-a-given-array-using-fisher-yates-shuffle-algorithm/
        for (int i = positions.Count - 1; i > 0; i--) // percorre a lista de trás para a frente
        {
            int j = Random.Range(0, i + 1); // escolhe um índice aleatório entre 0 e i (inclusive)
            (positions[i], positions[j]) = (positions[j], positions[i]); // troca os elementos nas posições i e j
        }

        // atribuir cada posição baralhada a cada puzzle e configurar o dígito
        for (int i = 0; i < puzzles.Count; i++) // para cada puzzle
        {
            puzzles[i].assignedCodePosition = positions[i]; // atribui a posição do código (0, 1, 2, ...) ao puzzle, de forma baralhada
            int digit = finalCode[positions[i]]; // obtém o dígito correspondente à posição atribuída ao puzzle
            Debug.Log($"[EscapeRoom] Puzzle '{puzzles[i].puzzleId}' ficou com o dígito na posição {positions[i]} (valor: {digit})"); // debug
            
            // notificar o puzzle do seu dígito para que configure a sua lógica interna
            puzzles[i].SetupCodeDigit(digit);
        }
    }
}
#endregion