using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;

/// <summary>
/// Classe utilizada para lidar com âncoras do MRUK (Mixed Reality Utility Kit)
/// 
/// Esta classe fornece métodos auxiliares para interpretar as âncoras detetadas pelo MRUK, 
/// identificando superfícies e objetos (labels) detetados no espaço físico do jogador
/// 
/// O MRUK é um conjunto de ferramentas e recursos que facilita a criação de aplicações com 
/// noção espacial, permitindo aos developers interagir com o mundo físico através da leitura
/// do cenário, colocação de conteúdo virtual e auxiliares visuais
/// 
/// MRUK: https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview/
/// </summary>
public static class MRUKAnchorUtils
{
    /// <summary>
    /// Dicionário de "Fallback"
    /// 
    /// Se o spawner pedir um determinado tipo de superfície e esta não existir 
    /// na lista de prioridades primárias ou for demasiado complexa de mapear, este 
    /// dicionário converte a label pedida numa alternativa viável
    /// </summary>
    private static readonly Dictionary<string, string> fallbackMap = new Dictionary<string, string>()
    {
        { "LAMP",    "OTHER"     }, // se for pedido um candeeiro, trata-o como um obstáculo genérico ("OTHER")
        { "PLANT",   "FLOOR"     }, // se for pedida uma planta, converte para "FLOOR" (chão) como superfície de base
    };

    /// <summary>
    /// Resolve a label de uma âncora verificando se existe um substituto 
    /// (fallback) definido no dicionário
    /// </summary>
    /// <param name="label">A label original pedida pelo sistema de spawn</param>
    /// <returns>A label substituída (se existir) ou a label original</returns>
    public static string ResolveFallback(string label)
    {
        // verifica se a chave existe no dicionário de fallback
        if (fallbackMap.ContainsKey(label))
            return fallbackMap[label]; // devolve a alternativa mapeada
        
        return label; // caso contrário, mantém a label original
    }

    /// <summary>
    /// Verifica se uma dada âncora é uma parede visível e não uma parede invisível do sistema
    /// 
    /// O MRUK por vezes cria "INVISIBLE_WALL_FACE" para fechar a geometria de uma sala, 
    /// e não é suposto os puzzles ficarem instanciados em paredes invisíveis.
    /// </summary>
    /// <param name="anchor">A âncora a analisar</param>
    /// <returns>Verdadeiro se for uma parede física real onde o jogador possa interagir</returns>
    public static bool IsVisibleWallFace(MRUKAnchor anchor)
    {
        if (anchor == null) return false; // se a âncora for nula, não é uma parede visível
        
        // se a âncora não tiver a label de "WALL_FACE", então não é uma parede, 
        // logo não é uma parede visível
        if (!anchor.HasAnyLabel(MRUKAnchor.SceneLabels.WALL_FACE)) return false;
        
        // retorna verdadeiro apenas se a âncora for uma parede e não tiver 
        // a label de "INVISIBLE_WALL_FACE"
        return !anchor.HasAnyLabel(MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE);
    }

    /// <summary>
    /// Este método define uma hierarquia de prioridade para forçar a identificação da âncora 
    /// como um único tipo principal, facilitando o sistema de spawn
    /// </summary>
    /// <param name="anchor">A âncora proveniente da sala do MRUK</param>
    /// <returns>A string com a designação prioritária da âncora</returns>
    public static string GetPrimaryLabel(MRUKAnchor anchor)
    {
        // se for uma parede real visível, assume logo que é WALL_FACE
        if (IsVisibleWallFace(anchor)) return "WALL_FACE";

        // define uma lista de prioridades, onde cada entrada é um par de 
        // (chave personalizada, label do MRUK)
        // permite mapear múltiplas labels do MRUK para uma única chave personalizada,
        // simplificando o sistema de spawn para os tipos de superfície e objetos mais comuns
        (string key, MRUKAnchor.SceneLabels sceneLabel)[] priority = {
            ("DESK", MRUKAnchor.SceneLabels.TABLE),
            ("BED", MRUKAnchor.SceneLabels.BED),
            ("COUCH", MRUKAnchor.SceneLabels.COUCH),
            ("STORAGE", MRUKAnchor.SceneLabels.STORAGE),
            ("SCREEN", MRUKAnchor.SceneLabels.SCREEN),
            ("LAMP", MRUKAnchor.SceneLabels.LAMP),
            ("PLANT", MRUKAnchor.SceneLabels.PLANT),
            ("WINDOW_FRAME", MRUKAnchor.SceneLabels.WINDOW_FRAME),
            ("DOOR_FRAME", MRUKAnchor.SceneLabels.DOOR_FRAME),
            ("FLOOR", MRUKAnchor.SceneLabels.FLOOR),
            ("CEILING", MRUKAnchor.SceneLabels.CEILING),
            ("OTHER", MRUKAnchor.SceneLabels.OTHER),
            ("WALL_ART", MRUKAnchor.SceneLabels.WALL_ART)
        };

        // itera sobre a lista
        foreach (var entry in priority)
        {
            // se a âncora tiver a label do MRUK correspondente, 
            // devolve-se a chave personalizada em string
            if (anchor.HasAnyLabel(entry.sceneLabel)) return entry.key;
        }
        
        // se a âncora não pertencer a nenhuma das categorias acima, devolve-se nulo
        return null;
    }

    /// <summary>
    /// Procura na sala do jogador por uma âncora que coincida exatamente com a label pedida,
    /// garantindo que ignora geometria invisível do MRUK
    /// </summary>
    /// <param name="room">A sala atual gerada e mapeada pelo MRUK</param>
    /// <param name="targetLabel">O tipo de superfície que estamos à procura</param>
    /// <returns>A primeira âncora válida encontrada ou nulo</returns>
    public static MRUKAnchor FindAnchorWithLabel(MRUKRoom room, string targetLabel)
    {
        // percorre todas as âncoras detetadas pelo scan da sala do jogador
        foreach (var anchor in room.Anchors)
        {
            // ignora paredes invisíveis criadas nativamente pelo SDK para fechar a sala
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE))
                continue;

            // se se estiver à procura de uma parede, usa a função para garantir que é visível
            if (targetLabel == "WALL_FACE" && !IsVisibleWallFace(anchor))
                continue;

            // extrai a label principal
            string primaryLabel = GetPrimaryLabel(anchor);
            
            // se a label bater certo com o que se procura, encontra-se a âncora
            if (primaryLabel == targetLabel) return anchor;
        }
        
        return null; // não se encontrou a âncora pedida
    }

    /// <summary>
    /// Função que procura quadros na parede ("WALL_ART") que ainda não tenham sido utilizados
    /// 
    /// Isto permite ter múltiplos puzzles baseados em quadros na mesma sala física, 
    /// sem que se sobreponham no mesmo quadro do mundo real
    /// </summary>
    /// <param name="room">A sala atual mapeada</param>
    /// <param name="usedWallArtAnchors">Um HashSet com as âncoras de arte que já estão ocupadas</param>
    /// <returns>Uma âncora de WALL_ART livre, ou nulo se não houver quadros disponíveis</returns>
    public static MRUKAnchor FindUnusedWallArtAnchor(MRUKRoom room, HashSet<MRUKAnchor> usedWallArtAnchors)
    {
        // itera por todas as âncoras da sala
        foreach (var anchor in room.Anchors)
        {
            // verifica se a âncora é um quadro de parede e se ainda não está na lista de usados
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.WALL_ART) && !usedWallArtAnchors.Contains(anchor))
                return anchor; // encontrou um quadro livre, logo, devolve a âncora
        }
        
        return null; // não se encontrou nenhum quadro disponível
    }
}
