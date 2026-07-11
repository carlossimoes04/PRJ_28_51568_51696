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
public static class ObstacleDetector
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
        MRUKAnchor.SceneLabels.WALL_ART | 
        MRUKAnchor.SceneLabels.WINDOW_FRAME | 
        MRUKAnchor.SceneLabels.DOOR_FRAME | 
        MRUKAnchor.SceneLabels.COUCH |
        MRUKAnchor.SceneLabels.OTHER;

    // distância máxima para considerar que um obstáculo está a obstruir uma célula da parede (em metros)
    private const float maxDistLimit = 0.85f;

    /// <summary>
    /// Método utilizado para marcar células de uma grelha de parede como 
    /// impedidas por obstáculos na sala do jogador
    /// </summary>
    /// <param name="grid">Grelha de parede</param>
    /// <param name="allAnchors">Lista de âncoras</param>
    /// <param name="margin">Margem de segurança</param>
    public static void MarkObstructedCells(WallGrid grid, List<MRUKAnchor> allAnchors, float margin)
    {
        if (grid.Cells == null) return; // se não houver células, não há nada a marcar

        foreach (var anchor in allAnchors) // iterar sobre todas as âncoras na sala
        {
            if (anchor == grid.ParentAnchor) continue; // ignorar a âncora da própria parede

            if (anchor.HasAnyLabel(ObstacleLabels)) // se a âncora for um obstáculo
            {
                if (anchor.VolumeBounds.HasValue) // se a âncora tiver limites de volume (Bounds)
                {
                    Bounds bounds = anchor.VolumeBounds.Value; // obter os limites do obstáculo
                    bounds.Expand(margin * 2f); // expandir os limites para incluir a margem de segurança
                    // marcar as células da grelha que estão dentro dos limites 
                    // expandidos do obstáculo como ocupadas
                    MarkCellsByBounds(grid, anchor.transform, bounds);
                }
                else if (anchor.PlaneRect.HasValue) // se a âncora tiver um plano (Rect)
                {
                    Rect rect = anchor.PlaneRect.Value; // obter o retângulo do obstáculo
                    // expandir o retângulo para incluir a margem de segurança
                    Rect expandedRect = new Rect(rect.x - margin, rect.y - margin, rect.width + margin * 2f, rect.height + margin * 2f);
                    // marcar as células da grelha que estão dentro do retângulo 
                    // expandido do obstáculo como ocupadas
                    MarkCellsByPlane(grid, anchor.transform, expandedRect);
                }
            }
        }
    }

    /// <summary>
    /// Marca células da grelha de parede como ocupadas se estiverem dentro dos limites de um obstáculo
    /// ou se estiverem a uma distância menor que maxDistLimit do obstáculo
    /// </summary>
    /// <param name="grid">Grelha de parede</param>
    /// <param name="obstacleTransform">Transformação do obstáculo</param>
    /// <param name="localBounds">Limites locais do obstáculo</param>
    private static void MarkCellsByBounds(WallGrid grid, Transform obstacleTransform, Bounds localBounds)
    {
        // iterar sobre todas as células da grelha
        for (int x = 0; x < grid.Columns; x++)
        {
            for (int y = 0; y < grid.Rows; y++)
            {
                if (!grid.Cells[x, y].isFree) continue; // ignorar células já ocupadas

                // converter a posição da célula de world space para local space
                /*
                é necessário converter as coordenadas da célula de world space para local space, porque
                os limites do obstáculo (localBounds) estão definidos no seu próprio espaço local

                desta forma, pode-se usar o método Contains() do Bounds para verificar se a célula 
                está dentro dos limites do obstáculo
                */
                Vector3 cellWorldPos = grid.Cells[x, y].worldPosition;
                Vector3 localPointInObstacle = obstacleTransform.InverseTransformPoint(cellWorldPos);

                // se a célula estiver dentro dos limites do obstáculo
                if (localBounds.Contains(localPointInObstacle))
                {
                    grid.Cells[x, y].isFree = false; // marcar a célula como ocupada
                }
                else // se não estiver dentro dos limites do obstáculo
                {
                    // verificar se a célula está a uma distância menor que maxDistLimit do obstáculo
                    // para isso, lançar um raio a partir da célula na direção da parede e verificar
                    // se o raio intersecta os limites do obstáculo
                    Vector3 wallNormal = grid.ParentAnchor.transform.forward;
                    Vector3 localDir = obstacleTransform.InverseTransformDirection(wallNormal);

                    /*
                    projetam-se dois raios a partir da célula:
                    - rayFwd: aponta para a frente (interior da sala) para detetar 
                    móveis normais encostados à parede
                    - rayBack: aponta para trás (interior da parede) para detetar 
                    bounding boxes recuadas que estejam um pouco dentro da parede

                    o rayBack foi implementado mais por segurança
                    */
                    Ray rayFwd = new Ray(localPointInObstacle, localDir);
                    Ray rayBack = new Ray(localPointInObstacle, -localDir);

                    // se algum dos raios intersectar os limites do obstáculo a uma distância menor que maxDistLimit
                    if (localBounds.IntersectRay(rayFwd, out float distFwd) && distFwd < maxDistLimit)
                        grid.Cells[x, y].isFree = false; // marcar a célula como ocupada
                    else if (localBounds.IntersectRay(rayBack, out float distBack) && distBack < maxDistLimit)
                        grid.Cells[x, y].isFree = false; // marcar a célula como ocupada
                }
            }
        }
    }

    /// <summary>
    /// Marca células da grelha de parede como ocupadas se estiverem dentro do retângulo de um obstáculo
    /// </summary>
    /// <param name="grid">Grelha de parede</param>
    /// <param name="obstacleTransform">Transform do obstáculo</param>
    /// <param name="localRect">Retângulo local do obstáculo</param>
    private static void MarkCellsByPlane(WallGrid grid, Transform obstacleTransform, Rect localRect)
    {
        // iterar sobre todas as células da grelha
        for (int x = 0; x < grid.Columns; x++)
        {
            for (int y = 0; y < grid.Rows; y++)
            {
                if (!grid.Cells[x, y].isFree) continue; // ignorar células já ocupadas

                // converter a posição da célula de world space para local space
                /*
                é necessário converter as coordenadas da célula de world space para local space, porque
                os limites do obstáculo (localBounds) estão definidos no seu próprio espaço local

                desta forma, pode-se usar o método Contains() do Bounds para verificar se a célula 
                está dentro dos limites do obstáculo
                */
                Vector3 cellWorldPos = grid.Cells[x, y].worldPosition;
                Vector3 localPointInObstacle = obstacleTransform.InverseTransformPoint(cellWorldPos);

                // verifica se o plano está encostado ou próximo da parede (tolerância até 60cm) 
                // antes de marcar a célula como ocupada
                if (Mathf.Abs(localPointInObstacle.z) <= 0.6f)
                {
                    // se a célula estiver dentro do retângulo do obstáculo
                    if (localRect.Contains(new Vector2(localPointInObstacle.x, localPointInObstacle.y)))
                    {
                        grid.Cells[x, y].isFree = false; // marcar a célula como ocupada
                    }
                }
            }
        }
    }
}
#endregion