using UnityEngine;
using TMPro;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Puzzle da estante de livros
/// 
/// Este puzzle é responsável por revelar um dígito do código final
/// 
/// Procura nos livros da estante qual tem o nome do artista cuja pintura saiu fake,
/// e mostra na capa desse livro o dígito correto do finalCode
/// 
/// Os outros livros mostram dígitos falsos para confundir
///
/// O dígito que este puzzle revela é atribuído automaticamente pelo EscapeRoomManager
/// através do sistema assignedCodePosition + SetupCodeDigit()
///
/// Configuração no Inspector:
///   - spineTexts: TextMeshPro das lombadas (nomes dos artistas)
///   - coverTexts: TextMeshPro das capas (onde aparece o dígito)
///     IMPORTANTE: Os índices devem coincidir (spineTexts[0] e coverTexts[0] = mesmo livro)
/// </summary>
public class BookShelfPuzzle : BasePuzzle
{
    [Header("Livros")]
    [Tooltip("TextMeshPro das lombadas (com o nome do artista) - índice deve coincidir com coverTexts")]
    public TextMeshPro[] spineTexts;

    [Tooltip("TextMeshPro das capas (onde aparece o dígito do código) - índice deve coincidir com spineTexts")]
    public TextMeshPro[] coverTexts;

    [Header("Configuração Visual")]
    [Tooltip("Se for true, esconde os dígitos das capas até o puzzle ser ativado")]
    public bool hideDigitsUntilActive = true;

    // índice do livro correto (com o artista fake) na lista de livros
    private int correctBookIndex = -1;

    /// <summary>
    /// Inicializa a estante de livros detetando qual é o livro que corresponde 
    /// ao artista falso
    /// 
    /// - É chamado pelo PuzzleSpawner durante a fase de criação de elementos na sala
    /// - Lê a tag do artista fake a partir do PuzzleSpawner para identificar o livro correto
    /// </summary>
    /// <param name="anchor">Âncora associada ao puzzle</param>
    public override void Initialize(MRUKAnchor anchor)
    {
        puzzleId = "wall_bookshelf_01"; // id do puzzle
        // procura o PuzzleSpawner na cena para obter a tag do artista fake
        PuzzleSpawner spawner = FindAnyObjectByType<PuzzleSpawner>();
        // se não encontrar o PuzzleSpawner ou a tag do artista fake estiver vazia
        // mostra um debug na consola
        if (spawner == null || string.IsNullOrEmpty(spawner.fakeArtistTag))
        {
            Debug.LogWarning("[BookShelfPuzzle] PuzzleSpawner não encontrado ou fakeArtistTag vazio");
            return;
        }

        // obtém a tag do artista fake a partir do PuzzleSpawner
        string fakeArtist = spawner.fakeArtistTag;

        // percorre os textos das lombadas para encontrar o índice do livro que corresponde ao artista fake
        for (int i = 0; i < spineTexts.Length; i++)
        {
            if (spineTexts[i] == null) continue; // ignora se o TextMeshPro da lombada estiver vazio

            // obtém o texto da lombada e remove espaços em branco no início e no fim
            string spineText = spineTexts[i].text.Trim();
            // compara o texto da lombada com o nome do artista fake (ignorando maiúsculas/minúsculas)
            if (string.Equals(spineText, fakeArtist.Trim(), System.StringComparison.OrdinalIgnoreCase))
            { // se encontrar o livro correto
                correctBookIndex = i; // armazena o índice do livro correto
                Debug.Log($"[BookShelfPuzzle] livro do artista fake encontrado no índice {i}: {spineText}");
                break;
            }
        }

        if (correctBookIndex < 0) // se não encontrou nenhum livro com o artista fake
        {
            Debug.LogWarning($"[BookShelfPuzzle] nenhum livro encontrado com o artista {fakeArtist}");
        }

        // se hideDigitsUntilActive for true, esconde os dígitos das capas até o puzzle ser ativado
        // isto está sempre ativo, pois os puzzles são todos ativados ao mesmo tempo
        if (hideDigitsUntilActive)
        {
            SetCoverDigitsVisible(false);
        }
    }

    /// <summary>
    /// Atribui o número do código que este puzzle deve revelar ao jogador
    /// 
    /// Integração:
    /// - Chamado pelo EscapeRoomManager logo a seguir à geração do código final da sala
    /// - Passa a este puzzle o dígito específico que ele ficou responsável por guardar
    /// </summary>
    /// <param name="digit">Um dos dígitos do código final</param>
    public override void SetupCodeDigit(int digit)
    {
        Debug.Log($"[BookShelfPuzzle] dígito atribuído: {digit} (posição {assignedCodePosition} do código final)");
        ConfigureCoverDigits(digit);
    }

    /// <summary>
    /// Ativa o puzzle, revelando os dígitos das capas dos livros
    /// </summary>
    public override void Activate()
    {
        Debug.Log($"[BookShelfPuzzle] puzzle ativado - o livro correto é o índice {correctBookIndex}");
        
        // se hideDigitsUntilActive for true, mostra os dígitos das capas agora que o puzzle foi ativado
        // isto está sempre ativo, pois os puzzles são todos ativados ao mesmo tempo
        if (hideDigitsUntilActive)
        {
            SetCoverDigitsVisible(true);
        }
    }

    /// <summary>
    /// Coloca o dígito correto na capa do livro do artista fake,
    /// e dígitos aleatórios (diferentes do correto) nas capas dos outros livros
    /// </summary>
    private void ConfigureCoverDigits(int correctDigit)
    {
        // itera sobre todos os livros da estante
        for (int i = 0; i < coverTexts.Length; i++)
        {
            if (coverTexts[i] == null) continue; // ignora se o TextMeshPro da capa estiver vazio

            if (i == correctBookIndex) // se for o livro do artista fake
            {
                coverTexts[i].text = correctDigit.ToString(); // coloca o dígito correto na capa
                Debug.Log($"[BookShelfPuzzle] cover[{i}] (artista fake): dígito correto é {correctDigit}");
            }
            else // se for qualquer outro livro
            {
                int decoy = correctDigit; // inicia o dígito decoy com o valor correto
                while (decoy == correctDigit) // enquanto o dígito decoy for igual ao correto
                {
                    decoy = Random.Range(0, 10); // gera um dígito aleatório entre 0 e 9
                }
                coverTexts[i].text = decoy.ToString(); // coloca o dígito decoy na capa
                Debug.Log($"[BookShelfPuzzle] cover[{i}]: dígito decoy é {decoy}");
            }
        }
    }

    /// <summary>
    /// Mostra ou esconde todos os dígitos das capas
    /// </summary>
    private void SetCoverDigitsVisible(bool visible)
    {
        foreach (var cover in coverTexts)
        {
            if (cover != null)
                cover.gameObject.SetActive(visible);
        }
    }
}
