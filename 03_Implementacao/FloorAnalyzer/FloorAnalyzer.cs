using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;

#region #ai_assisted

/// <summary>
/// Classe responsável por analisar o chão de uma sala, criando uma grelha de células que representam 
/// áreas livres e ocupadas, permitindo a deteção de zonas adequadas para posicionamento de objetos
/// </summary>
public class FloorAnalyzer : MonoBehaviour
{
    [Header("Configurações de Grelha no Chão")]
    [Tooltip("Resolução da grelha no chão em metros (ex: 0.1 = quadrados de 10x10 cm)")]
    public float gridCellResolution = 0.1f;
    
    [Tooltip("Margem de segurança forçada em torno dos móveis assentes no chão (em metros)")]
    public float obstaclePadding = 0.2f;

    [Header("Debugging")]
    public bool showGizmos = true;
    public Color freeCellColor = new Color(0, 1, 0, 0.4f);
    public Color occupiedCellColor = new Color(1, 0, 0, 0.4f);

    // dicionário que mapeia âncoras de chão para as duas respetivas grelhas de células, 
    // permitindo a análise de áreas livres e ocupadas
    private Dictionary<MRUKAnchor, FloorGrid> floorGrids = new Dictionary<MRUKAnchor, FloorGrid>();

    /// <summary>
    /// Este método analisa o chão da sala fornecida, criando uma grelha de células 
    /// para cada âncora de chão encontrada
    /// </summary>
    /// <param name="room">Sala a ser analisada</param>
    public void AnalyzeFloor(MRUKRoom room)
    {
        floorGrids.Clear(); // limpa o dicionário de grelhas antes de iniciar a análise

        if (room == null) return; // retorna se a sala não for fornecida

        // percorre todas as âncoras da sala, procurando por âncoras de chão
        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.FLOOR)) // se a âncora for do tipo "chão"
            {
                // cria uma grelha de células para a âncora de chão, com base na resolução definida
                FloorGrid grid = new FloorGrid(anchor, gridCellResolution);
                // marca as células ocupadas com base nos obstáculos detectados, aplicando o padding definido
                FloorObstacleDetector.MarkObstructedCells(grid, room.Anchors, obstaclePadding);
                // adiciona a grelha ao dicionário, associando-a à âncora correspondente
                floorGrids[anchor] = grid;
            }
        }
    }

    /// <summary>
    /// Verifica se uma área específica da grelha está livre, considerando o tamanho necessário do puzzle
    /// </summary>
    /// <param name="grid">Grelha a ser analisada</param>
    /// <param name="startX">Coordenada X do início da área</param>
    /// <param name="startY">Coordenada Y do início da área</param>
    /// <param name="requiredCols">Número de colunas necessárias</param>
    /// <param name="requiredRows">Número de linhas necessárias</param>
    /// <returns></returns>
    private bool IsAreaFree(FloorGrid grid, int startX, int startY, int requiredCols, int requiredRows)
    {
        // verifica se a área solicitada está dentro dos limites da grelha
        if (startX + requiredCols > grid.Columns || startY + requiredRows > grid.Rows) 
            return false;

        // percorre as células da área solicitada, verificando se todas estão livres
        for (int x = 0; x < requiredCols; x++)
        {
            for (int y = 0; y < requiredRows; y++)
            {
                if (!grid.Cells[startX + x, startY + y].isFree) 
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Obtém uma posição aleatória dentro de uma zona livre suficiente na grelha do chão,
    /// considerando o tamanho necessário do puzzle
    /// 
    /// Retorna null se não houver zona suficiente
    /// </summary>
    /// <param name="anchor">Âncora da grelha a ser analisada</param>
    /// <param name="requiredSize">Tamanho necessário da área</param>
    /// <returns></returns>
    public Vector3? GetRandomSufficientZone(MRUKAnchor anchor, Vector2 requiredSize)
    {
        // verifica se a âncora fornecida possui uma grelha associada no dicionário
        if (!floorGrids.TryGetValue(anchor, out FloorGrid grid)) return null;

        // calcula o número de colunas e linhas necessárias 
        // com base no tamanho requerido e na resolução da grelha
        int requiredCols = Mathf.Max(1, Mathf.CeilToInt(requiredSize.x / grid.CellSize));
        int requiredRows = Mathf.Max(1, Mathf.CeilToInt(requiredSize.y / grid.CellSize));

        // lista para armazenar os centros das áreas livres válidas encontradas
        List<Vector3> validBlockCenters = new List<Vector3>();

        // percorre a grelha, verificando todas as posições iniciais 
        // possíveis para a área necessária
        for (int x = 0; x <= grid.Columns - requiredCols; x++)
        {
            for (int y = 0; y <= grid.Rows - requiredRows; y++)
            {
                // verifica se a área a partir da posição (x, y) é livre
                if (IsAreaFree(grid, x, y, requiredCols, requiredRows))
                {
                    // posição do canto superior esquerdo da área livre
                    Vector3 startCellPos = grid.Cells[x, y].worldPosition;
                    // posição do canto inferior direito da área livre 
                    Vector3 endCellPos = grid.Cells[x + requiredCols - 1, y + requiredRows - 1].worldPosition;
                    // calcula o centro da área livre
                    Vector3 centerPos = (startCellPos + endCellPos) / 2f;
                    // adiciona o centro da área livre à lista de centros válidos
                    validBlockCenters.Add(centerPos);
                }
            }
        }

        // se houver áreas livres válidas, escolhe uma 
        // aleatoriamente e retorna o seu centro
        if (validBlockCenters.Count > 0)
        {
            // retorna o centro de uma área livre aleatória
            return validBlockCenters[Random.Range(0, validBlockCenters.Count)];
        }

        return null; // retorna null se não houver áreas livres suficientes
    }

#if UNITY_EDITOR
    /// <summary>
    /// Desenha gizmos na cena para visualização das células livres e ocupadas na grelha do chão
    /// </summary>
    private void OnDrawGizmos()
    {
        // se a visualização de gizmos estiver desativada ou se não houver grelhas, retorna
        if (!showGizmos || floorGrids == null) return;

        // define o tamanho do cubo a ser desenhado para cada célula, com base na resolução da grelha
        Vector3 cubeSize = new Vector3(gridCellResolution * 0.85f, gridCellResolution * 0.85f, 0.015f);

        // percorre todas as grelhas de chão armazenadas no dicionário
        foreach (var grid in floorGrids.Values)
        {
            if (grid.Cells == null) continue; // se a grelha não tiver células, passa para a próxima

            // percorre todas as células da grelha
            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    // obtém a célula atual da grelha
                    FloorGridCell cell = grid.Cells[x, y];
                    // define a cor do gizmo com base no estado da célula (livre ou ocupada)
                    Gizmos.color = cell.isFree ? freeCellColor : occupiedCellColor;
                    // cria uma matriz de transformação para posicionar o gizmo corretamente no mundo
                    Matrix4x4 rotationMatrix = Matrix4x4.TRS(cell.worldPosition, grid.ParentAnchor.transform.rotation, Vector3.one);
                    Gizmos.matrix = rotationMatrix;
                    // desenha um cubo representando a célula no chão
                    Gizmos.DrawCube(Vector3.zero, cubeSize);
                }
            }
        }
        // faz reset da matriz de gizmos para a identidade, evitando que afete 
        // outros gizmos desenhados posteriormente
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
#endregion