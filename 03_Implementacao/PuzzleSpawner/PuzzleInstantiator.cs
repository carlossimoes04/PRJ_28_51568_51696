using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;

#region #my_code
/// <summary>
/// Classe responsável por instanciar puzzles em posições exatas, 
/// considerando o tipo de superfície e ajustando a 
/// escala para se adequar a pinturas ou paredes específicas
/// </summary>
public static class PuzzleInstantiator
{
    /// <summary>
    /// Instancia um prefab de puzzle em uma posição exata, ajustando rotação e escala 
    /// conforme o tipo de superfície
    /// </summary>
    /// <param name="prefab">Prefab do puzzle a ser instanciado</param>
    /// <param name="anchor">Âncora que define a superfície de ancoragem</param>
    /// <param name="label">Rótulo do tipo de superfície</param>
    /// <param name="exactSpawnPos">Posição exata para instanciar o puzzle</param>
    /// <param name="requiredSize">Tamanho necessário do puzzle</param>
    /// <param name="wallSpawnOffset">Offset para afastar o puzzle da parede</param>
    /// <param name="playerCamera">Câmera do jogador</param>
    /// <param name="activePuzzles">Dicionário de puzzles ativos</param>
    /// <returns>GameObject instanciado</returns>
    public static GameObject SpawnExactPrefab(
        GameObject prefab, 
        MRUKAnchor anchor, 
        string label, 
        Vector3 exactSpawnPos,
        Vector2 requiredSize,
        float wallSpawnOffset,
        Camera playerCamera,
        Dictionary<MRUKAnchor, GameObject> activePuzzles)
    {

        if (prefab == null) return null; // retorna null se o prefab não for fornecido

        Vector3 finalPos = exactSpawnPos; // posição final para instanciar o puzzle

        // se for uma parede ou uma pintura
        if (label == "WALL_FACE" || label == "WALL_ART")
        {
            // ajusta a posição para colocar o puzzle ficar a "olhar" para dentro da sala,
            // afastando-o da parede para evitar clipping
            finalPos += PuzzlePlacementSolver.GetWallOutwardDirection(anchor, playerCamera) * wallSpawnOffset;
        }

        Quaternion spawnRot; // rotação final para instanciar o puzzle

        switch (label) // define a rotação com base no tipo de superfície
        {
            case "FLOOR": // se for o chão, mantém a rotação padrão
                spawnRot = Quaternion.identity; 
                break;
            case "CEILING": // se for o teto, gira 180 graus em torno do eixo X para ficar de cabeça para baixo
                spawnRot = Quaternion.Euler(180, 0, 0); 
                break;
            case "WALL_FACE": // se for uma parede
            case "WALL_ART": // e se for uma pintura
                // combina a rotação da âncora com a rotação do prefab para alinhar corretamente o puzzle à parede
                spawnRot = anchor.transform.rotation * prefab.transform.rotation;
                break;
            default: // para outros tipos de superfície, mantém a rotação padrão da âncora
                spawnRot = anchor.transform.rotation; 
                break;
        }

        // instancia o prefab na posição e rotação calculadas
        GameObject puzzleInstance = Object.Instantiate(prefab, finalPos, spawnRot);

        // armazena a escala original do prefab para manter a proporção correta
        Vector3 originalScale = prefab.transform.localScale; 
        
        // define o pai do puzzle instanciado como a âncora para manter a hierarquia organizada
        puzzleInstance.transform.SetParent(anchor.transform);

        if (label != "WALL_ART") // se não for uma pintura, mantém a escala original do prefab
        {
            puzzleInstance.transform.localScale = originalScale;
        }
        
        // adiciona o puzzle instanciado ao dicionário de puzzles ativos, associando-o à âncora correspondente
        activePuzzles[anchor] = puzzleInstance;

        if (label == "WALL_ART") // se for uma pintura
        {
            // obtém o retângulo real da pintura a partir da âncora
            Rect realPainting = anchor.PlaneRect.Value;
            
            // cria um GameObject vazio para servir como contêiner de escala, 
            // permitindo ajustar a escala do puzzle em relação à pintura
            GameObject scalerContainer = new GameObject(prefab.name + "_Scaler");
            scalerContainer.transform.SetParent(anchor.transform);
            
            // posiciona o contâiner de escala no centro da pintura, 
            // mantendo a posição Z do puzzle instanciado
            scalerContainer.transform.localPosition = new Vector3(
                realPainting.center.x,
                realPainting.center.y,
                puzzleInstance.transform.localPosition.z
            );

            // faz reset da rotação e escala do contêiner de escala para evitar distorções
            scalerContainer.transform.localRotation = Quaternion.identity;
            scalerContainer.transform.localScale = Vector3.one;

            // define o puzzle instanciado como filho do contêiner de escala,
            // permitindo que a escala do contâiner afete o puzzle
            puzzleInstance.transform.SetParent(scalerContainer.transform);
            puzzleInstance.transform.localPosition = Vector3.zero;

            // ajusta a escala do contêiner de escala com base no tamanho real da pintura 
            // e no tamanho necessário do puzzle
            if (requiredSize.x > 0.01f && requiredSize.y > 0.01f) 
            {
                // calcula os multiplicadores de escala para os eixos X e Y
                float scaleMultiplierX = realPainting.width / requiredSize.x;
                float scaleMultiplierY = realPainting.height / requiredSize.y;

                // aplica a escala calculada ao contâiner de escala, mantendo o eixo Z em 1 para evitar distorções
                scalerContainer.transform.localScale = new Vector3(scaleMultiplierX, scaleMultiplierY, 1f);
            }
        }

        // inicializa o componente BasePuzzle do puzzle instanciado, se existir
        BasePuzzle puzzleComponent = puzzleInstance.GetComponentInChildren<BasePuzzle>();
        if (puzzleComponent != null) puzzleComponent.Initialize(anchor);
        
        Debug.Log($"[PuzzleInstantiator] criação de {prefab.name} em {label} concluída");
        
        return puzzleInstance;
    }
}
#endregion