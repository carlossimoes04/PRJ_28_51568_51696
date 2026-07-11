/*
 * A implementação deste script baseia-se nas seguintes referências:
 * 
 * - Verificação de pontos dentro de limites (Bounds.Contains):
 *   https://discussions.unity.com/t/test-to-see-if-a-vector3-point-is-within-a-boxcollider/17385
 *   https://discussions.unity.com/t/bounds-contains-is-not-working-at-all-as-expected/671128/7
 * 
 * - Deteção de interseção de raios com limites 3D (Bounds.IntersectRay):
 *   https://discussions.unity.com/t/how-do-i-find-the-intercept-with-bounds/255796
 */

using UnityEngine;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;

#region #ai_assisted

/// <summary>
/// Classe responsável por detectar obstáculos no chão e marcar as células da grelha como ocupadas ou livres
/// </summary>
public static class FloorObstacleDetector
{
    /// <summary>
    /// Labels de âncoras que representam obstáculos na sala do jogador, se existirem
    /// 
    /// É readonly porque são constantes e não devem ser alteradas em tempo de execução
    /// </summary>
    public static readonly MRUKAnchor.SceneLabels ObstacleLabels = 
        MRUKAnchor.SceneLabels.STORAGE | 
        MRUKAnchor.SceneLabels.SCREEN | 
        MRUKAnchor.SceneLabels.BED | 
        MRUKAnchor.SceneLabels.TABLE | 
        MRUKAnchor.SceneLabels.LAMP | 
        MRUKAnchor.SceneLabels.PLANT | 
        MRUKAnchor.SceneLabels.COUCH |
        MRUKAnchor.SceneLabels.OTHER;

    // altura máxima para considerar que um obstáculo está em cima de uma célula do chão (em metros)
    private const float maxHeightLimit = 2.0f;

    /// <summary>
    /// Marca as células da grelha do chão como ocupadas se houver 
    /// obstáculos detectados nas âncoras MRUK fornecidas
    /// </summary>
    /// <param name="grid">Grelha do chão</param>
    /// <param name="allAnchors">Lista de âncoras</param>
    /// <param name="margin">Margem para a detecção de obstáculos</param>
    public static void MarkObstructedCells(FloorGrid grid, List<MRUKAnchor> allAnchors, float margin)
    {
        if (grid.Cells == null) return; // se não houver células na grelha, sai do método

        foreach (var anchor in allAnchors) // itera sobre todas as âncoras fornecidas
        {
            if (anchor == grid.ParentAnchor) continue; // ignora a âncora associada à grelha do chão

            if (anchor.HasAnyLabel(ObstacleLabels)) // verifica se a âncora possui algum dos rótulos de obstáculos
            {
                if (anchor.VolumeBounds.HasValue) // verifica se a âncora possui limites de volume definidos
                {
                    Bounds bounds = anchor.VolumeBounds.Value; // obtém os limites de volume da âncora
                    bounds.Expand(margin * 2f); // expande os limites de volume com base na margem fornecida
                    // marca as células da grelha como ocupadas com base nos limites de volume
                    MarkCellsByBounds(grid, anchor.transform, bounds); 
                }
                else if (anchor.PlaneRect.HasValue) // verifica se a âncora possui um retângulo de plano definido
                {
                    Rect rect = anchor.PlaneRect.Value; // obtém o retângulo de plano da âncora
                    // expande o retângulo com base na margem fornecida
                    Rect expandedRect = new Rect(rect.x - margin, rect.y - margin, rect.width + margin * 2f, 
                                                rect.height + margin * 2f); 
                    // marca as células da grelha como ocupadas com base no retângulo expandido
                    MarkCellsByPlane(grid, anchor.transform, expandedRect);
                }
            }
        }
    }

    /// <summary>
    /// Se a âncora tiver limites de volume (como COUCH, BED, TABLE, etc), marca as células da grelha como 
    /// ocupadas se estiverem dentro desses limites
    /// </summary>
    /// <param name="grid">Grelha do chão</param>
    /// <param name="obstacleTransform">Transform da âncora do obstáculo</param>
    /// <param name="localBounds">Limites locais do obstáculo</param>
    private static void MarkCellsByBounds(FloorGrid grid, Transform obstacleTransform, Bounds localBounds)
    {
        // itera sobre todas as células da grelha do chão
        for (int x = 0; x < grid.Columns; x++)
        {
            for (int y = 0; y < grid.Rows; y++)
            {
                if (!grid.Cells[x, y].isFree) continue; // ignora células que já estão marcadas como ocupadas

                // obtém a posição da célula em world space e converte para local space do obstáculo
                /*
                é necessário converter as coordenadas da célula de world space para local space, porque
                os limites do obstáculo (localBounds) estão definidos no seu próprio espaço local

                desta forma, pode-se usar o método Contains() do Bounds para verificar se a célula 
                está dentro dos limites do obstáculo
                */
                Vector3 cellWorldPos = grid.Cells[x, y].worldPosition;
                Vector3 localPointInObstacle = obstacleTransform.InverseTransformPoint(cellWorldPos);

                // verifica se o ponto local da célula está dentro dos limites do obstáculo
                if (localBounds.Contains(localPointInObstacle))
                {
                    grid.Cells[x, y].isFree = false; // marca a célula como ocupada
                }
                else // se a célula não estiver dentro dos limites do obstáculo, verifica se há um obstáculo acima dela
                {
                    /*
                    este segundo teste foi feito porque existem obstáculos suspensos na sala (como 
                    armários de parede, prateleiras ou TVs) que não tocam no chão

                    ao testar o espaço livre acima da célula, evita-se colocar objetos virtuais 
                    debaixo de móveis ou a atravessar objetos reais que estejam suspensos
                    */

                    // normal da grelha do chão (direção para cima)
                    Vector3 normal = grid.ParentAnchor.transform.forward;
                    
                    // alinha a normal com a rotação do obstáculo no seu espaço local
                    Vector3 localDir = obstacleTransform.InverseTransformDirection(normal);
                    // cria um raio a partir da posição local da célula para cima (direção da normal)
                    Ray rayUp = new Ray(localPointInObstacle, localDir);

                    // verifica se o raio lançado para cima a partir da célula intersecta os limites do obstáculo
                    if (localBounds.IntersectRay(rayUp, out float distUp) && distUp < maxHeightLimit)
                    {
                        // marca a célula como ocupada se houver um obstáculo acima 
                        // dela dentro da profundidade máxima
                        grid.Cells[x, y].isFree = false; 
                    }
                }
            }
        }
    }

    /// <summary>
    /// Este método marca as células da grelha como ocupadas se estiverem dentro do 
    /// retângulo do plano de um obstáculo (como DOOR_FRAME, WINDOW_FRAME, WALL_ART, etc)
    /// </summary>
    /// <param name="grid">Grelha do chão</param>
    /// <param name="obstacleTransform">Transform do obstáculo</param>
    /// <param name="localRect">Retângulo no espaço local do obstáculo</param>
    private static void MarkCellsByPlane(FloorGrid grid, Transform obstacleTransform, Rect localRect)
    {
        // itera sobre todas as células da grelha do chão
        for (int x = 0; x < grid.Columns; x++)
        {
            for (int y = 0; y < grid.Rows; y++)
            {
                if (!grid.Cells[x, y].isFree) continue; // ignora células que já estão marcadas como ocupadas

                // obtém a posição da célula em world space e converte para local space do obstáculo
                /*
                é necessário converter as coordenadas da célula de world space para local space, porque
                os limites do obstáculo (localBounds) estão definidos no seu próprio espaço local

                desta forma, pode-se usar o método Contains() do Bounds para verificar se a célula 
                está dentro dos limites do obstáculo
                */
                Vector3 cellWorldPos = grid.Cells[x, y].worldPosition;
                Vector3 localPointInObstacle = obstacleTransform.InverseTransformPoint(cellWorldPos);

                // verifica se a célula está dentro do retângulo do plano do obstáculo
                // 0.2f é a tolerância para considerar que a célula está "em cima" do plano do obstáculo
                if (Mathf.Abs(localPointInObstacle.z) <= 0.2f)
                {
                    // verifica se o ponto local da célula está dentro do retângulo do obstáculo
                    if (localRect.Contains(new Vector2(localPointInObstacle.x, localPointInObstacle.y)))
                    {
                        grid.Cells[x, y].isFree = false; // marca a célula como ocupada
                    }
                }
            }
        }
    }
}
#endregion