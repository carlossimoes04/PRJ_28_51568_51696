using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;

#region #my_code
/// <summary>
/// Coordena todo o fluxo de spawning
/// 
/// Decide que cenários de enigmas vão ser usados na sessão de jogo, valida o espaço disponível 
/// nas paredes através dos analisadores e utiliza:
/// - MRUKAnchorUtils para localizar âncoras espaciais com labels específicos (WALL_FACE, FLOOR, CEILING, WALL_ART)
/// - PrefabSizeCalculator para calcular o tamanho de cada prefab, caso não seja fornecido um tamanho customizado
/// - PuzzleInstantiator para executar a instanciação física do prefab, aplicando rotação, 
/// offset de profundidade e ajustes de escala
/// - PuzzlePlacementSolver para calcular a posição exata de spawn de cada prefab
/// 
/// Esta classe foi implementada para centralizar e automatizar o ciclo de distribuição física dos enigmas
/// no início de cada sessão, garantindo que o jogo se adapta dinamicamente à geometria e tamanho do 
/// quarto real do utilizador
/// </summary>
public class PuzzleSpawner : MonoBehaviour
{
    [Header("Referências")]
    public Transform cameraRig;
    public Camera playerCamera;

    [System.Serializable]
    public class PuzzleElement
    {
        [Tooltip("A etiqueta (label) da âncora do MRUK onde o elemento será colocado (ex: WALL_FACE)")]
        public string targetLabel; 

        [Tooltip("O prefab 3D do objeto a ser instanciado")]
        public GameObject prefab;

        [Tooltip("Se ativado, o elemento será forçado a nascer à altura dos olhos do jogador")]
        public bool spawnAtPlayerHeight = false;

        [Tooltip("Se ativado, o prefab vai tentar fazer spawn o mais alto possível na parede (perto do teto)")]
        public bool spawnNearCeiling = false;

        [Tooltip("Identificador do artista usado se o elemento for um quadro (WALL_ART)")]
        public string artistTag;

        [Tooltip("Versão falsa deste prefab. De momento só é usado para WALL_ART, mas pode ser usado para outros tipos de puzzle no futuro")]
        public GameObject fakePrefab;

        [Tooltip("Se maior que (0,0), ignora o tamanho real do modelo e força este tamanho na procura de parede")]
        public Vector2 customSizeOverride = Vector2.zero;
        
        [HideInInspector]
        public Vector2 requiredSize; // armazena o tamanho 3D final necessário para o spawn
    }

    /// <summary>
    /// Representa um cenário de um puzzle, contendo um grupo de elementos associados
    /// </summary>
    [System.Serializable]
    public class PuzzleScenario
    {
        [Tooltip("Nome deste cenário")]
        public string scenarioName; 
        [Tooltip("Lista de elementos a serem instanciados neste cenário")]
        public List<PuzzleElement> elementsToSpawn; 
    }


    /// <summary>
    /// Agrupa uma lista de cenários sob uma mesma categoria (ex: puzzles de parede)
    /// </summary>
    [System.Serializable]
    public class ScenarioCategory
    {
        [Tooltip("Título ou nome da categoria")]
        public string categoryTitle; 
        [Tooltip("Lista de cenários possíveis para esta categoria")]
        public List<PuzzleScenario> scenarios;
    }

    [Header("Configurações de Cenários")]
    [Tooltip("Se ativado, todos os cenários de cada categoria são instanciados em vez de sortear apenas um")]
    public bool spawnAllScenarios = true;

    [Tooltip("Número mínimo de cenários (variações) exigido por cada label/categoria")]
    public int minScenariosRequired = 1;

    [Header("Spawn")]
    [Tooltip("Distância em metros para afastar prefabs em WALL_FACE da superfície da parede")]
    [Min(0f)] public float wallSpawnOffset = 0.06f;

    [Tooltip("Margem de segurança (em metros) à volta de cada puzzle instanciado na parede para evitar que outros fiquem colados")]
    [Min(0f)] public float puzzleSpacingPadding = 0.3f;

    [Tooltip("Lista de todas as categorias de cenários configuradas no jogo")]
    public List<ScenarioCategory> categories;

     // guarda a associação entre as âncoras e os GameObjects virtuais instanciados
    public Dictionary<MRUKAnchor, GameObject> activePuzzles = new();

    // referência ao analisador de paredes
    private WallAnalyzer wallAnalyzer;

    // referência ao analisador de chão
    private FloorAnalyzer floorAnalyzer;

    // referência à sala física do jogador
    private MRUKRoom currentRoom;

    // lista de paredes utilizáveis (com espaço livre suficiente) para instanciar puzzles
    private List<WallGrid> usableWalls;

    // conjunto que impede a reutilização da mesma âncora de quadro (WALL_ART)
    // é HashSet porque a verificação de existência é mais rápida do que numa List
    private HashSet<MRUKAnchor> usedWallArtAnchors = new();

    // variável que guarda a tag do artista do quadro falso (fake) sorteado, caso exista
    [HideInInspector] public string fakeArtistTag;

    /// <summary>
    /// Método inicial executado antes do Start
    /// 
    /// Valida se as categorias cumprem o limite mínimo configurado
    /// 
    /// Esta função foi implementada para realizar uma verificação no editor, alertando caso 
    /// alguma categoria de puzzles tenha menos variações do que as necessárias para o jogo
    /// </summary>
    void Awake()
    {
        foreach (var category in categories)
        {
            if (category.scenarios.Count < minScenariosRequired)
            {
                Debug.LogWarning($"[EscapeRoom] a categoria {category.categoryTitle} só tem {category.scenarios.Count} cenários, mas o mínimo exigido é {minScenariosRequired}");
            }
        }
    }

    /// <summary>
    /// Inicializa e faz o spawn de todos os puzzles e objetos da sala com base 
    /// nas categorias e cenários de configurados
    /// 
    /// Esta função foi implementada para centralizar a lógica de spawn da sala
    /// 
    /// Esta recebe os analisadores, obtém as paredes utilizáveis, escolhe os cenários 
    /// que devem ser instanciados nesta sessão (sejam todos ou sorteados) e aciona o resto da cadeia
    /// </summary>
    /// <param name="room">A sala física do jogador</param>
    /// <param name="wAnalyzer">O analisador de paredes</param>
    /// <param name="fAnalyzer">O analisador de chão</param>
    /// <returns>Lista de todos os puzzles instanciados nesta sessão</returns>
    public List<BasePuzzle> InitializePuzzles(MRUKRoom room, WallAnalyzer wAnalyzer, FloorAnalyzer fAnalyzer)
    {
        wallAnalyzer = wAnalyzer; // guarda a referência ao analisador de paredes
        floorAnalyzer = fAnalyzer; // guarda a referência ao analisador de chão
        currentRoom = room; // guarda a referência à sala do jogador
        
        List<BasePuzzle> spawnedPuzzles = new(); // lista final de puzzles instanciados nesta sessão
        usedWallArtAnchors.Clear(); // limpa a lista de âncoras de quadros usadas para evitar reutilização
        
        // obtém a lista de paredes utilizáveis (com espaço livre suficiente) para instanciar puzzles
        usableWalls = wallAnalyzer.GetUsableWalls();

        foreach (var category in categories) // percorre cada categoria de cenários configurada
        {
            // se a categoria não tiver cenários, ignora e passa para a próxima
            if (category.scenarios == null || category.scenarios.Count == 0) continue;
            // lista de cenários que serão instanciados nesta categoria
            List<PuzzleScenario> scenariosToSpawn;
            // se a opção de spawnAllScenarios estiver ativada
            if (spawnAllScenarios)
            {
                // instancia todos os cenários da categoria
                scenariosToSpawn = category.scenarios;
                Debug.Log($"[EscapeRoom] a instanciar todos os {scenariosToSpawn.Count} cenários");
            }
            else // se a opção de spawnAllScenarios estiver desativada
            {
                // sorteia um cenário aleatório da categoria para instanciar
                int randomIndex = Random.Range(0, category.scenarios.Count);
                // cria uma lista com apenas o cenário sorteado
                scenariosToSpawn = new List<PuzzleScenario> { category.scenarios[randomIndex] };
                Debug.Log($"[EscapeRoom] sorteado cenário: {scenariosToSpawn[0].scenarioName}");
            }

            foreach (var chosenScenario in scenariosToSpawn) // percorre cada cenário escolhido para instanciar
            {
                // tenta instanciar o cenário completo, verificando se todos os elementos podem ser colocados
                if (TrySpawnScenario(chosenScenario, out List<BasePuzzle> scenarioSpawnedPuzzles))
                {
                    // só adiciona os puzzles à lista definitiva se todos os elementos foram gerados
                    spawnedPuzzles.AddRange(scenarioSpawnedPuzzles);
                }
            }
        }
        return spawnedPuzzles;
    }
    /// <summary>
    /// Tenta processar e instanciar um cenário completo
    /// 
    /// Retorna falso e reverte todas as criações caso algum elemento falhe por falta de espaço físico
    /// </summary>
    /// <param name="scenario">O cenário a ser processado</param>
    /// <param name="scenarioPuzzles">Lista de puzzles instanciados com sucesso</param>
    /// <returns>Verdadeiro se todos os elementos foram instanciados, falso caso contrário</returns>
    private bool TrySpawnScenario(PuzzleScenario scenario, out List<BasePuzzle> scenarioPuzzles)
    {
        Debug.Log($"[EscapeRoom] a processar cenário: {scenario.scenarioName}");
        // lista de objetos instanciados fisicamente no cenário
        List<GameObject> scenarioSpawnedObjects = new List<GameObject>();
        // lista de puzzles instanciados com sucesso no cenário
        scenarioPuzzles = new List<BasePuzzle>();
        
        // determinar o falso/intruso
        List<int> fakeEligible = new(); // lista de índices de elementos que têm versão fake configurada

        // percorre todos os elementos do cenário para identificar quais têm versão fake
        for (int e = 0; e < scenario.elementsToSpawn.Count; e++)
        {
            if (scenario.elementsToSpawn[e].fakePrefab != null)
                fakeEligible.Add(e);
        }

        // sorteia um elemento falso/intruso aleatório da lista de elegíveis
        int fakeChosenIndex = fakeEligible.Count > 0 ? fakeEligible[Random.Range(0, fakeEligible.Count)] : -1;

        if (fakeChosenIndex >= 0) // se existir um elemento falso sorteado
        {
            // guarda a tag do artista do quadro falso sorteado para referência futura
            fakeArtistTag = scenario.elementsToSpawn[fakeChosenIndex].artistTag; 
            Debug.Log($"[EscapeRoom] o 'intruso' será o elemento {fakeChosenIndex} ({scenario.elementsToSpawn[fakeChosenIndex].prefab.name} -> {scenario.elementsToSpawn[fakeChosenIndex].fakePrefab.name}), artista: {fakeArtistTag}");
        }

        // percorre todos os elementos do cenário para tentar instanciá-los
        for (int elementIdx = 0; elementIdx < scenario.elementsToSpawn.Count; elementIdx++)
        {
            // obtém o elemento atual e determina se é o falso/intruso
            PuzzleElement element = scenario.elementsToSpawn[elementIdx];
            bool isFake = (elementIdx == fakeChosenIndex);

            // escolhe o prefab a instanciar (real ou falso)
            GameObject prefabToSpawn = isFake ? element.fakePrefab : element.prefab;

            // calcula o tamanho físico necessário para o prefab, usando override manual se configurado
            CalculateElementRequiredSize(element, prefabToSpawn);

            // tenta encontrar a âncora e a posição exata para instanciar o elemento
            if (TryFindAnchorAndPosition(element, out MRUKAnchor targetAnchor, out Vector3 exactSpawnPos, out string resolvedLabel))
            {
                GameObject spawnedObj = PuzzleInstantiator.SpawnExactPrefab(
                    prefabToSpawn, 
                    targetAnchor, 
                    resolvedLabel, 
                    exactSpawnPos,
                    element.requiredSize,
                    wallSpawnOffset,
                    playerCamera,
                    activePuzzles); // instancia o prefab na posição exata calculada

                if (spawnedObj != null) // se a instanciação foi bem-sucedida
                {
                    // adiciona o objeto instanciado à lista de objetos do cenário
                    scenarioSpawnedObjects.Add(spawnedObj);
                    // tenta obter o componente BasePuzzle do objeto instanciado
                    BasePuzzle newPuzzle = spawnedObj.GetComponent<BasePuzzle>();
                    // se o componente BasePuzzle existir, adiciona à lista de puzzles do cenário
                    if (newPuzzle != null) scenarioPuzzles.Add(newPuzzle);
                    
                    // se o elemento for WALL_FACE e o analisador de paredes estiver disponível
                    if (resolvedLabel == "WALL_FACE" && wallAnalyzer != null)
                    {
                        // marca a área ocupada na parede para evitar colisões com outros puzzles
                        wallAnalyzer.MarkAreaAsOccupied(targetAnchor, spawnedObj.transform.position, element.requiredSize, puzzleSpacingPadding);
                    }
                }
                else // se a instanciação falhou, para a geração o cenário e reverte os objetos já criados
                {
                    Debug.LogWarning($"[EscapeRoom] parar cenário {scenario.scenarioName} porque falhou a criação de {element.prefab.name}");
                    RollbackScenario(scenarioSpawnedObjects); // destrói todos os objetos já instanciados do cenário
                    return false;
                }
            }
            else // se não encontrou âncora ou posição suficiente para o elemento
            {
                Debug.LogWarning($"[EscapeRoom] parar cenário {scenario.scenarioName} porque falhou o spawn de {element.prefab.name} (sem espaço livre na parede)");
                RollbackScenario(scenarioSpawnedObjects); // destrói todos os objetos já instanciados do cenário
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Calcula o tamanho físico necessário para o prefab, usando override manual se configurado
    /// </summary>
    /// <param name="element">O elemento do cenário</param>
    /// <param name="prefabToSpawn">O prefab a ser instanciado</param>
    private void CalculateElementRequiredSize(PuzzleElement element, GameObject prefabToSpawn)
    {
        // se o elemento tiver um tamanho customizado definido
        if (element.customSizeOverride.x > 0.01f && element.customSizeOverride.y > 0.01f)
        {
            // usa o tamanho customizado fornecido, ignorando o tamanho real do prefab
            element.requiredSize = element.customSizeOverride;
        }
        else // se não houver tamanho customizado
        {
            // calcula o tamanho real do prefab usando a função utilitária
            element.requiredSize = PrefabSizeCalculator.Calculate(prefabToSpawn);
        }
        Debug.Log($"[EscapeRoom] tamanho final para o {element.prefab.name}: {element.requiredSize}");
    }

    /// <summary>
    /// Encaminha o elemento para a âncora certa com base na label
    /// </summary>
    /// <param name="element">O elemento do cenário</param>
    /// <param name="targetAnchor">A âncora encontrada para o elemento</param>
    /// <param name="exactPos">A posição exata calculada para o spawn do elemento</param>
    /// <param name="resolvedLabel">A label resolvida para a âncora</param>
    /// <returns>Verdadeiro se encontrou âncora e posição, falso caso contrário</returns>
    private bool TryFindAnchorAndPosition(PuzzleElement element, out MRUKAnchor targetAnchor, out Vector3 exactPos, out string resolvedLabel)
    {
        // resolve a label do elemento, considerando possíveis fallback se a label original não estiver disponível
        resolvedLabel = MRUKAnchorUtils.ResolveFallback(element.targetLabel);
        targetAnchor = null; // inicializa a âncora alvo como nula
        Vector3? foundPos = null; // inicializa a posição encontrada como nula

        switch (resolvedLabel) // determina a âncora e posição com base na label resolvida
        {
            case "WALL_FACE": // caso seja uma parede normal
                // tenta encontrar a melhor posição na parede com base na altura do jogador ou no teto
                TryGetBestWallPosition(element, out targetAnchor, out foundPos);
                break;
            case "FLOOR": // caso seja chão
            case "CEILING": // caso seja teto
                // tenta encontrar uma âncora com a label correspondente na sala do jogador
                targetAnchor = MRUKAnchorUtils.FindAnchorWithLabel(currentRoom, resolvedLabel);
                break;
            case "WALL_ART": // caso seja um quadro
                // tenta encontrar uma âncora de quadro não utilizada na sala do jogador
                targetAnchor = MRUKAnchorUtils.FindUnusedWallArtAnchor(currentRoom, usedWallArtAnchors);
                if (targetAnchor != null) // se encontrou uma âncora de quadro válida
                {
                    usedWallArtAnchors.Add(targetAnchor); // marca a âncora como usada para evitar reutilização
                }
                else // se não encontrou âncora de quadro
                {
                    resolvedLabel = "WALL_FACE"; // fallback para parede normal
                    // tenta encontrar a melhor posição na parede com base na altura do jogador ou no teto
                    TryGetBestWallPosition(element, out targetAnchor, out foundPos);
                }
                break;
            default: // caso seja outra label (como COUCH, TABLE, etc.) | isto foi implementado para permitir futuras extensões do sistema de spawn
                // tenta encontrar uma âncora com a label correspondente na sala do jogador
                targetAnchor = MRUKAnchorUtils.FindAnchorWithLabel(currentRoom, resolvedLabel);
                break;
        }

        // se encontrou a âncora mas ainda não tem a posição exata
        if (targetAnchor != null && !foundPos.HasValue)
        {
            // tenta calcular a posição exata de spawn usando o PuzzlePlacementSolver, 
            // considerando a âncora, label resolvida, tamanho necessário e os analisadores de parede e chão
            foundPos = PuzzlePlacementSolver.GetSpawnPosition(targetAnchor, resolvedLabel, element.requiredSize, wallAnalyzer, floorAnalyzer);
        }

        // se encontrou âncora e posição
        if (targetAnchor != null && foundPos.HasValue)
        {
            exactPos = foundPos.Value; // atribui a posição exata encontrada
            return true;
        }
        else // se não encontrou âncora ou posição
        {
            exactPos = Vector3.zero; // atribui uma posição padrão (zero)
            return false;
        }
    }

    /// <summary>
    /// Pesquisa a melhor posição na parede para o elemento, considerando, ou não, a altura do jogador ou teto
    /// </summary>
    /// <param name="element">O elemento do cenário</param>
    /// <param name="bestAnchor">A âncora de parede encontrada</param>
    /// <param name="bestPos">A posição exata calculada para o spawn do elemento</param>
    private void TryGetBestWallPosition(PuzzleElement element, out MRUKAnchor bestAnchor, out Vector3? bestPos)
    {
        bestAnchor = null; // inicializa a âncora alvo como nula
        bestPos = null; // inicializa a posição encontrada como nula

        if (usableWalls.Count == 0) return; // se não houver paredes utilizáveis, retorna imediatamente
        int bestWallIndex = -1; // índice da melhor parede encontrada (-1 indica que nenhuma foi encontrada)

        // se o elemento tiver que spawnar à altura do jogador ou perto do teto
        if (element.spawnAtPlayerHeight || element.spawnNearCeiling)
        {
            // inicializa a diferença mínima de altura como o valor máximo possível 
            // para garantir que qualquer altura encontrada será menor
            float minHeightDiff = float.MaxValue;
            // obtém o tamanho final necessário para o elemento
            Vector2 finalSize = element.requiredSize; 

            // percorre todas as paredes utilizáveis para encontrar a melhor posição
            for (int i = 0; i < usableWalls.Count; i++)
            {
                MRUKAnchor testAnchor = usableWalls[i].ParentAnchor; // obtém a âncora da parede atual
                float targetY = 0f; // inicializa a altura alvo para o spawn do elemento
                Vector3? testPos = null; // inicializa a posição de teste como nula

                // se o elemento spawnar perto do teto e o jogo souber a altura dessa parede
                if (element.spawnNearCeiling && testAnchor.PlaneRect.HasValue)
                {
                    // calcula a posição inicial de teste baseada na altura máxima da parede
                    Vector3 topLocal = new Vector3(0, testAnchor.PlaneRect.Value.yMax, 0);
                    // converte a posição local da âncora para coordenadas globais
                    Vector3 topGlobal = testAnchor.transform.TransformPoint(topLocal);
                    // calcula a altura alvo inicial para o spawn do elemento, considerando 
                    // o tamanho do prefab e um pequeno offset de 10cm
                    float startY = topGlobal.y - (finalSize.y / 2f) - 0.1f;
                    
                    // tenta encontrar espaço livre descendo 15cm de cada vez (até 5 tentativas)
                    /*
                    foram 5 tentativas e não 10, por exemplo, porque não faz sentido descer muito,
                    pois o objetivo é colocar o puzzle perto do teto
                    */
                    for (int attempt = 0; attempt < 5; attempt++)
                    {
                        targetY = startY - (attempt * 0.15f); // desce 15cm a cada tentativa
                        // tenta encontrar uma posição livre na parede à altura alvo calculada
                        testPos = wallAnalyzer.GetRandomPositionAtHeight(testAnchor, targetY, finalSize);
                        if (testPos.HasValue) break;
                    }
                }
                else // se o elemento spawnar à altura do jogador
                {
                    // obtém a altura do jogador usando o PuzzlePlacementSolver
                    targetY = PuzzlePlacementSolver.GetPlayerHeight(playerCamera, cameraRig);
                    // tenta encontrar uma posição livre na parede à altura do jogador
                    testPos = wallAnalyzer.GetRandomPositionAtHeight(testAnchor, targetY, finalSize);
                }
                if (testPos.HasValue) // se encontrou uma posição válida na parede
                {
                    // calcula a diferença entre a altura da posição encontrada e a altura alvo
                    float diff = Mathf.Abs(testPos.Value.y - targetY);
                    
                    if (diff < minHeightDiff) // se a diferença for menor que a mínima encontrada até agora
                    {
                        minHeightDiff = diff; // atualiza a diferença mínima de altura
                        bestWallIndex = i; // atualiza o índice da melhor parede encontrada
                        bestPos = testPos; // atualiza a melhor posição encontrada
                    }
                }
            }
        }
        else // se o elemento não precisar de spawnar à altura do jogador nem perto do teto
        {
            // percorre todas as paredes utilizáveis
            for (int i = 0; i < usableWalls.Count; i++)
            {
                // tenta encontrar uma posição aleatória suficiente na parede para o elemento, considerando o tamanho necessário
                Vector3? testPos = wallAnalyzer.GetRandomSufficientZone(usableWalls[i].ParentAnchor, element.requiredSize);

                if (testPos.HasValue) // se encontrou uma posição válida na parede
                {
                    bestWallIndex = i; // atualiza o índice da melhor parede encontrada
                    bestPos = testPos; // atualiza a melhor posição encontrada
                    break;
                }
            }
        }

        if (bestWallIndex != -1) // se encontrou uma parede válida com espaço suficiente para o elemento
        {
            bestAnchor = usableWalls[bestWallIndex].ParentAnchor; // atribui a âncora da melhor parede encontrada
        }
    }

    /// <summary>
    /// Destrói os objetos de um cenário que falhou a meio da instanciação, 
    /// evitando que fiquem puzzles incompletos na cena
    /// </summary>
    /// <param name="spawnedObjects">Lista de objetos instanciados do cenário</param>
    private void RollbackScenario(List<GameObject> spawnedObjects)
    {
        foreach (var obj in spawnedObjects) // percorre todos os objetos instanciados do cenário
        {
            if (obj != null) // se o objeto ainda existir na cena
            {
                Destroy(obj); // destrói o objeto para reverter a instanciação
            }
        }
        Debug.LogWarning($"[EscapeRoom] todos os objetos do cenário foram destruídos");
    }
}
#endregion