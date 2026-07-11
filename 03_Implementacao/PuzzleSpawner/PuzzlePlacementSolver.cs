using UnityEngine;
using Meta.XR.MRUtilityKit;

#region #my_code
// Calcula as coordenadas exatas onde os puzzles devem ser posicionados na grelha das paredes ou do chão, 
// garantindo que aparecem à altura dos olhos do jogador e sem flutuar
public static class PuzzlePlacementSolver
{
    /// <summary>
    /// Método estático para obter a altura atual do jogador (coordenada Y)
    /// </summary>
    /// <param name="playerCamera">Câmara do jogador</param>
    /// <param name="cameraRig">Rig da câmara</param>
    /// <returns>Altura atual do jogador</returns>
    public static float GetPlayerHeight(Camera playerCamera, Transform cameraRig)
    {
        if (playerCamera != null) // se existir uma câmara do jogador
        {
            return playerCamera.transform.position.y; // retorna a coordenada Y da posição da câmara do jogador
        }

        if (cameraRig != null) // se a rig da câmara for fornecida como fallback
        {
            Camera cam = cameraRig.GetComponentInChildren<Camera>(); // obtém a câmara dentro da rig
            if (cam != null) // se a câmara for encontrada dentro da rig
            {
                return cam.transform.position.y; // retorna a coordenada Y da posição da câmara encontrada na rig
            }
        }

        // retorna uma altura padrão (1.4f) se nenhuma câmara válida for encontrada, 
        // assumindo uma altura média de um jogador em pé
        return Camera.main != null ? Camera.main.transform.position.y : 1.4f;
    }

    /// <summary>
    /// Método para calcular a direção normalizada que aponta para fora da parede, relativamente ao jogador
    /// Garante que o objeto colocado não fique "dentro" da parede ou virado para o lado errado
    /// </summary>
    /// <param name="anchor">Anchor da parede</param>
    /// <param name="playerCamera">Câmara do jogador</param>
    /// <returns>Direção para fora da parede</returns>
    public static Vector3 GetWallOutwardDirection(MRUKAnchor anchor, Camera playerCamera)
    {
        // obtém a direção "forward" da anchor, que é a direção que a parede está a "olhar"
        Vector3 forward = anchor.transform.forward;

        // determina a posição do jogador: usa a câmara fornecida, ou a principal, 
        // ou calcula um ponto à frente da parede se falhar.
        Vector3 playerPos = playerCamera != null
            ? playerCamera.transform.position
            : (Camera.main != null ? Camera.main.transform.position : anchor.transform.position + forward);

        // calcula o vetor unitário que vai da posição da ancoragem até à posição do jogador
        Vector3 toPlayer = (playerPos - anchor.transform.position).normalized;

        /*
        calcula o produto escalar entre a frente da parede e a direção para o jogador:
        - se o resultado for >= 0, significa que a frente da parede já aponta para o jogador 
        (ou está perpendicular)
        - se for negativo, a frente da parede aponta para o lado oposto, então inverte-se
        a direção para garantir que o objeto colocado "olha" para a sala
        */
        // retorna a direção correta (forward ou -forward) para garantir que o objeto "olha" para a sala
        return Vector3.Dot(forward, toPlayer) >= 0f ? forward : -forward;
    }

    /// <summary>
    /// Método para obter a posição de spawn ideal para um puzzle, com base na label 
    /// da ancoragem e nos analisadores de paredes e chão
    /// </summary>
    /// <param name="anchor">Âncora da superfície</param>
    /// <param name="label">Label da ancoragem</param>
    /// <param name="requiredSize">Tamanho necessário</param>
    /// <param name="wallAnalyzer">Analisador de paredes</param>
    /// <param name="floorAnalyzer">Analisador de chão</param>
    /// <returns>Posição de spawn ideal</returns>
    public static Vector3 GetSpawnPosition(MRUKAnchor anchor, string label, Vector2 requiredSize, WallAnalyzer wallAnalyzer, FloorAnalyzer floorAnalyzer)
    {
        switch (label) // verifica a label para determinar o tipo de superfície
        {
            case "WALL_FACE": // se for uma parede
                if (wallAnalyzer != null) // se existir um analisador de paredes
                {
                    // tenta obter uma zona aleatória suficiente na parede para colocar o puzzle
                    Vector3? randomZone = wallAnalyzer.GetRandomSufficientZone(anchor, requiredSize);

                    // se for encontrada uma zona válida, retorna essa posição
                    if (randomZone.HasValue) return randomZone.Value;
                }
                return anchor.transform.position; // se não for possível encontrar uma zona válida, 
                // retorna a posição da ancoragem como fallback

            case "FLOOR": // se for o chão
                if (floorAnalyzer != null) // se existir um analisador de chão
                {
                    // tenta obter uma zona aleatória suficiente no chão para colocar o puzzle
                    Vector3? randomFloorZone = floorAnalyzer.GetRandomSufficientZone(anchor, requiredSize);

                    // se for encontrada uma zona válida, retorna essa posição
                    if (randomFloorZone.HasValue) return randomFloorZone.Value;
                }
                return anchor.transform.position; // se não for possível encontrar uma zona válida, 
                // retorna a posição da ancoragem como fallback

            case "CEILING": // se for teto (apesar de não ser esperado)
                return anchor.transform.position; // retorna a posição da ancoragem, 
                // assumindo que o puzzle será colocado diretamente no teto

            default: // para outras labels (apesar de só WALL_FACE e FLOOR serem esperadas)
                if (anchor.VolumeBounds.HasValue) // se a âncora tiver limites de volume definidos
                {
                    /*
                    calcula uma posição deslocada para a frente da ancoragem
                    o deslocamento é metade da profundidade (size.z) do volume, 
                    posicionando o objeto na "face" frontal do volume
                    */
                    return anchor.transform.position + anchor.transform.forward * anchor.VolumeBounds.Value.size.z * 0.5f;
                }
                return anchor.transform.position; // se não houver limites de volume, 
                // retorna a posição da ancoragem como fallback
        }
    }
}
#endregion