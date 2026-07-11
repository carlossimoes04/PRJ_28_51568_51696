using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;

#region #my_code
public class WirePuzzle : BasePuzzle
{
    /// <summary>
    /// Estrutura que representa uma ligação entre uma 
    /// porta de saída e uma porta de entrada
    /// 
    /// É struct porque representa um dado simples e leve,
    /// usado apenas para guardar a associação entre duas
    /// portas sem necessidade de herança ou comportamento
    /// 
    /// É serializable porque permite que o Unity mostre e
    /// guarde este tipo no Inspector, incluindo os seus
    /// campos públicos
    /// </summary>
    [System.Serializable]
    public struct WireConnection
    {
        // porta de saída usada na ligação
        public string outputPort; 
        // porta de entrada usada na ligação
        public string inputPort;  
    }

    [Header("config do puzzle")]
    [Tooltip("saídas que existem no quadro")]
    // lista das saídas que o puzzle usa
    public List<string> outputPorts = new List<string> { "A", "B", "C" };
    
    [Tooltip("entradas que existem no quadro")]
    // lista das entradas que o puzzle usa
    public List<string> inputPorts = new List<string> { "1", "2", "3" };

    // lista das ligações certas do puzzle
    [Tooltip("ligações certas que o puzzle espera do jogador (só se vê no inspetor em play mode)")]
    public List<WireConnection> correctConnections = new List<WireConnection>();

    [Header("texto no quadro")]
    [Tooltip("texto que aparece no ecrã do quadro")]
    // texto que mostra o estado do puzzle
    [SerializeField] private TMP_Text screenText;

    [Header("dependências")]
    [Tooltip("script que toca o morse")]
    // script que recebe a mensagem do morse
    public MorseCodePlayer morsePlayer;

    // dicionário que guarda as ligações ativas feitas pelo jogador
    private Dictionary<string, string> activeConnections = new Dictionary<string, string>();
    
    // dígito que este puzzle vai mostrar no fim
    private int revealedDigit = 0;

    // referência ao sistema de partículas a desligar quando se resolve o puzzle
    [SerializeField] private ParticleSystem sparksParticles;

    /// <summary>
    /// Configura o estado inicial do Wire Puzzle
    /// 
    /// Procura a coluna de som na cena (MorseCodePlayer), limpa o registo 
    /// de ligações elétricas e mostra a mensagem de avaria ("Need Repairs") 
    /// no ecrã
    /// </summary>
    /// <param name="anchor">Âncora da parede</param>
    public override void Initialize(MRUKAnchor anchor)
    {
        puzzleId = "wall_wire_01"; // id do puzzle

        if (morsePlayer == null)
        {
            // procurar na cena inteira quem tem o script MorseCodePlayer
            morsePlayer = FindAnyObjectByType<MorseCodePlayer>();
            
            if (morsePlayer == null)
                Debug.LogError("[Wire Puzzle] não foi encontrada a coluna de som");
            else
                Debug.Log("[Wire Puzzle] coluna encontrada e ligada");
        }

        // limpa as ligações ativas
        activeConnections.Clear();
        // cria uma entrada vazia para cada saída
        foreach (var output in outputPorts)
        {
            activeConnections.Add(output, "");
        }
        
        // mostra o estado inicial no ecrã
        AtualizarEcra("Need Repairs");
    }

    /// <summary>
    /// Ativa o puzzle, gerando as ligações certas aleatórias
    /// </summary>
    public override void Activate()
    {
        // gera as ligações certas quando o puzzle começa
        GenerateRandomConnections();
    }

    /// <summary>
    /// Define o dígito que este puzzle vai revelar no fim
    /// </summary>
    public override void SetupCodeDigit(int digit)
    {
        // guarda o dígito recebido
        revealedDigit = digit;
        // volta a mostrar erro no ecrã
        AtualizarEcra("Need Repairs"); // começa com erro até ficar certo
    }

    /// <summary>
    /// Gera aleatoriamente as ligações certas entre saídas e entradas
    /// </summary>
    private void GenerateRandomConnections()
    {
        // limpa a lista das ligações certas
        correctConnections.Clear();

        // copia as entradas para poder baralhar
        List<string> shuffledInputs = new List<string>(inputPorts);

        // baralha a lista com fisher-yates
        for (int i = shuffledInputs.Count - 1; i > 0; i--)
        {
            // escolhe uma posição aleatória
            int j = Random.Range(0, i + 1);
            // troca as duas posições
            (shuffledInputs[i], shuffledInputs[j]) = (shuffledInputs[j], shuffledInputs[i]);
        }

        /*
        basicamente, as entradas são baralhadas da seguinte forma:
        1. faz-se uma cópia da lista de entradas (1, 2, 3)
        2. percorre-se a lista de trás para a frente, e para cada posição i:
            a. escolhe-se uma posição aleatória j entre 0 e i
            b. troca-se os elementos das posições i e j
        3. no fim, o resultado é uma lista de entradas baralhada
        */

        // inicializa a mensagem que vai para o morse como vazia
        string morseMessage = "";

        // cria a ligação certa para cada saída
        for (int i = 0; i < outputPorts.Count; i++)
        {
            // guarda a ligação certa desta saída
            correctConnections.Add(new WireConnection 
            { 
                outputPort = outputPorts[i], 
                inputPort = shuffledInputs[i] 
            });

            // monta a mensagem que vai para o morse
            morseMessage += outputPorts[i] + shuffledInputs[i] + " "; 
        }

        /*
        o passo acima cria a mensagem que vai para o morse da seguinte maneira:
        1. inicializa a string morseMessage como vazia
        2. percorre a lista de saídas, e para cada saída i:
            a. associa a saída atual à entrada baralhada e guarda a ligação correta na lista
            b. junta à string a letra da saída, o número da entrada e um espaço (ex: "A2 ")
        3. no fim, o resultado é a mensagem completa que a coluna vai tocar (ex: "A2 B3 C1 ")
        */

        // mostra a mensagem gerada no console
        Debug.Log($"[Wire Puzzle] conexões geradas - o código morse é: {morseMessage}");

        // se não houver referência ao morsePlayer, procura na cena
        if (morsePlayer == null)
        {
            morsePlayer = FindAnyObjectByType<MorseCodePlayer>();
        }

        // se existir referência ao morsePlayer, envia a mensagem para o player do morse
        if (morsePlayer != null)
        {
            morsePlayer.AtualizarMensagem(morseMessage);
        }
    }

    /// <summary>
    /// Método chamado quando o jogador conecta um fio entre uma saída e uma entrada
    /// </summary>
    /// <param name="outputPort"></param>
    /// <param name="inputPort"></param>
    public void ConnectWire(string outputPort, string inputPort)
    {
        // não faz nada se o puzzle já estiver resolvido
        if (isSolved) return;

        // atualiza a ligação desta saída
        if (activeConnections.ContainsKey(outputPort))
        {
            // guarda a ligação feita pelo jogador
            activeConnections[outputPort] = inputPort;
            // mostra a ligação na consola
            Debug.Log($"[Wire Puzzle] ligação estabelecida: {outputPort} -> {inputPort}");
            // valida o estado do puzzle
            CheckPuzzleState();
        }
    }

    public void DisconnectWire(string outputPort)
    {
        // não faz nada se o puzzle já estiver resolvido
        if (isSolved) return;

        // limpa a ligação desta saída
        if (activeConnections.ContainsKey(outputPort))
        {
            // mostra a desconexão no console
            Debug.Log($"[Wire Puzzle] Tomada desconectada: {outputPort}");
            // remove a ligação guardada
            activeConnections[outputPort] = "";
            // volta a validar o estado do puzzle
            CheckPuzzleState();
        }
    }

    /// <summary>
    /// Verifica se as ligações feitas pelo jogador correspondem
    ///  às ligações certas do puzzle
    /// </summary>
    private void CheckPuzzleState()
    {
        // conta quantas ligações estão certas
        int correctCount = 0;

        // percorre as ligações certas todas
        foreach (var correct in correctConnections)
        {
            // obtém a ligação feita pelo jogador para esta saída
            if (activeConnections.TryGetValue(correct.outputPort, out string currentInput))
            {
                // compara a ligação feita com a correta
                if (currentInput == correct.inputPort)
                {
                    // soma mais uma ligação certa
                    correctCount++;
                }
            }
        }

        // confirma se tudo está certo
        if (correctCount == correctConnections.Count)
        {
            // se não houver referência ao morsePlayer, procura na cena
            if (morsePlayer == null) 
            {
                morsePlayer = FindAnyObjectByType<MorseCodePlayer>();
            }

            // interrompe o som do morse
            if (morsePlayer != null)
            {
                morsePlayer.PararMorse();
            }

            // criação da mensagem final do ecrã
            string msgSucesso = $"You fixed it.\nCod: {revealedDigit}";
            // mostra a mensagem final
            AtualizarEcra(msgSucesso);

            // parar o efeito de faíscas
            if (sparksParticles != null)
            {
                // terminar o sistema de partículas de faíscas
                sparksParticles.Stop();

                // parar o som das faíscas se existir
                AudioSource sparksAudio = sparksParticles.GetComponent<AudioSource>();
                if (sparksAudio != null)
                {
                    sparksAudio.Stop();
                }
            }

            // marca o puzzle como completo
            CompletePuzzle(); 
        }
        else // se ainda não está certo
        {
            // volta a mostrar erro se ainda não estiver certo
            AtualizarEcra("Need Repairs");
        }
    }

    /// <summary>
    /// Atualiza o texto do ecrã do quadro com a mensagem especificada
    /// </summary>
    /// <param name="mensagem">A mensagem a ser exibida</param>
    private void AtualizarEcra(string mensagem)
    {
        // escreve a mensagem no ecrã se existir o texto
        if (screenText != null)
        {
            screenText.text = mensagem;
        }
    }
}
#endregion