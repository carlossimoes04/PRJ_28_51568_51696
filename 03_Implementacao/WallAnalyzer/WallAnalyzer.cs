using UnityEngine;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;

#region #ai_assisted
/// <summary>
/// Classe responsável por analisar as paredes detectadas na sala e 
/// gerar uma grelha de células para cada parede
/// </summary>
public class WallAnalyzer : MonoBehaviour
{
    [Header("Configurações de Grelha")]
    [Tooltip("Resolução da grelha em metros (ex: 0.1 = 10x10 cm)")]
    public float gridCellResolution = 0.1f;
    
    [Tooltip("Margem de segurança em torno dos obstáculos (em metros)")]
    public float obstaclePadding = 0.1f;

    [Tooltip("Ratio mínimo de células livres (0 a 1) para considerar a parede utilizável")]
    public float minFreeRatio = 0.1f;

    [Header("Debugging")]
    public bool showGizmos = true;
    public Color freeCellColor = new Color(0, 1, 0, 0.5f);
    public Color occupiedCellColor = new Color(1, 0, 0, 0.5f);

    /// <summary>
    /// Dicionário que mapeia âncoras de parede para suas respectivas grelhas de células
    /// </summary>
    private Dictionary<MRUKAnchor, WallGrid> wallGrids = new Dictionary<MRUKAnchor, WallGrid>();

    /// <summary>
    /// Inicializa o WallAnalyzer com base na sala fornecida, gerando grelhas 
    /// para cada parede detectada e marcando células ocupadas por obstáculos
    /// </summary>
    /// <param name="room">Sala a ser analisada</param>
    public void Initialize(MRUKRoom room)
    {
        wallGrids.Clear(); // limpar grelhas anteriores

        if (room == null) return; // se a sala for nula, não há nada a analisar

        // iterar sobre todas as âncoras da sala
        foreach (var anchor in room.Anchors)
        {
            // considerar apenas âncoras que representam paredes
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.WALL_FACE) || 
                anchor.HasAnyLabel(MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE))
            {
                WallGrid grid = new WallGrid(anchor, gridCellResolution); // criar uma grelha para a parede
                // marcar células ocupadas por obstáculos na sala
                ObstacleDetector.MarkObstructedCells(grid, room.Anchors, obstaclePadding);
                
                wallGrids[anchor] = grid; // armazenar a grelha no dicionário
            }
        }
    }

    /// <summary>
    /// Retorna uma lista de grelhas de parede que possuem células livres suficientes para colocar um puzzle
    /// </summary>
    /// <returns>Lista de grelhas de parede utilizáveis</returns>
    public List<WallGrid> GetUsableWalls()
    {
        List<WallGrid> usableWalls = new List<WallGrid>(); // lista de grelhas de parede utilizáveis

        // iterar sobre todas as grelhas de parede
        foreach (var grid in wallGrids.Values)
        {
            // ignorar paredes invisíveis, pois não são utilizáveis para colocar puzzles
            if (grid.ParentAnchor.HasAnyLabel(MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE)) continue;

            int freeCount = 0; // contador de células livres
            int totalCount = grid.Columns * grid.Rows; // total de células na grelha

            if (totalCount == 0) continue; // se não houver células, não há nada a analisar

            // contar células livres na grelha
            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    if (grid.Cells[x, y].isFree) freeCount++;
                }
            }

            // se a proporção de células livres for maior ou igual ao mínimo definido, 
            // considerar a parede utilizável
            if ((float)freeCount / totalCount >= minFreeRatio)
            {
                usableWalls.Add(grid); // adicionar a grelha à lista de paredes utilizáveis
            }
        }

        // baralhar a lista de paredes utilizáveis
        // isto permite que a seleção de paredes seja aleatória, evitando escolher sempre a mesma parede
        for (int i = 0; i < usableWalls.Count; i++)
        {
            WallGrid temp = usableWalls[i]; // armazenar a grelha atual temporariamente
            // escolher um índice aleatório a partir do índice atual até o final da lista
            int randomIndex = Random.Range(i, usableWalls.Count);
            usableWalls[i] = usableWalls[randomIndex]; // trocar a grelha atual com a grelha aleatória
            usableWalls[randomIndex] = temp; // colocar a grelha temporária na posição aleatória
        }

        return usableWalls; // retornar a lista de paredes utilizáveis
    }

    /// <summary>
    /// Verifica se uma área retangular na grelha está livre (sem células ocupadas)
    /// </summary>
    /// <param name="grid">Grelha de parede</param>
    /// <param name="startX">Coordenada X do canto superior esquerdo da área</param>
    /// <param name="startY">Coordenada Y do canto superior esquerdo da área</param>
    /// <param name="requiredCols">Número de colunas requeridas</param>
    /// <param name="requiredRows">Número de linhas requeridas</param>
    /// <returns></returns>
    private bool IsAreaFree(WallGrid grid, int startX, int startY, int requiredCols, int requiredRows)
    {
        // verificar se a área solicitada está dentro dos limites da grelha
        if (startX + requiredCols > grid.Columns || startY + requiredRows > grid.Rows) 
            return false;

        // iterar sobre cada célula na área solicitada e verificar se está livre
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
    /// Retorna uma posição aleatória dentro de uma área livre na parede, 
    /// próxima a uma altura alvo e que acomode o tamanho necessário do puzzle
    /// </summary>
    /// <param name="anchor">Âncora da parede</param>
    /// <param name="targetY">Altura alvo</param>
    /// <param name="requiredSize">Tamanho necessário do puzzle</param>
    /// <returns></returns>
    public Vector3? GetRandomPositionAtHeight(MRUKAnchor anchor, float targetY, Vector2 requiredSize)
    {
        // se a âncora não estiver no dicionário de grelhas, retornar nulo
        if (!wallGrids.TryGetValue(anchor, out WallGrid grid)) return null;
        // se a grelha não tiver células, retornar nulo
        if (grid.Rows == 0 || grid.Columns == 0) return null;

        // calcular o número de colunas e linhas necessárias para acomodar o tamanho do puzzle
        int requiredCols = Mathf.Max(1, Mathf.CeilToInt(requiredSize.x / grid.CellSize));
        int requiredRows = Mathf.Max(1, Mathf.CeilToInt(requiredSize.y / grid.CellSize));

        // lista para armazenar os centros das áreas livres que atendem aos critérios
        List<Vector3> bestBlocks = new List<Vector3>();
        // variável para armazenar a menor diferença de altura encontrada
        float minDiff = float.MaxValue;

        // iterar sobre todas as células possíveis na grelha que podem acomodar o tamanho do puzzle
        for (int x = 0; x <= grid.Columns - requiredCols; x++)
        {
            for (int y = 0; y <= grid.Rows - requiredRows; y++)
            {
                // verificar se a área retangular na grelha está livre
                if (IsAreaFree(grid, x, y, requiredCols, requiredRows))
                {
                    // calcular a posição central da área livre em coordenadas world space
                    float startYPos = grid.Cells[0, y].worldPosition.y; // posição Y da célula superior da área
                    float endYPos = grid.Cells[0, y + requiredRows - 1].worldPosition.y; // posição Y da célula inferior da área
                    float blockCenterWorldY = (startYPos + endYPos) / 2f; // calcular a altura central da área livre

                    // calcular a diferença absoluta entre a altura central da área livre e a altura alvo
                    float diff = Mathf.Abs(blockCenterWorldY - targetY);
                    
                    // se a diferença for menor que a menor diferença encontrada até agora, 
                    // atualizar minDiff e limpar a lista de melhores blocos
                    if (diff < minDiff - 0.01f)
                    {
                        minDiff = diff;
                        bestBlocks.Clear();
                    }
                    
                    // se a diferença for aproximadamente igual à menor diferença encontrada,
                    // adicionar a posição central da área livre à lista de melhores blocos
                    if (Mathf.Abs(diff - minDiff) <= 0.01f)
                    {
                        // calcular a posição central da área livre em coordenadas world space
                        Vector3 startCellPos = grid.Cells[x, y].worldPosition;
                        Vector3 endCellPos = grid.Cells[x + requiredCols - 1, y + requiredRows - 1].worldPosition;
                        Vector3 centerPos = (startCellPos + endCellPos) / 2f;

                        bestBlocks.Add(centerPos); // adicionar a posição central à lista de melhores blocos
                    }
                }
            }
        }

        // se houver blocos livres que atendem aos critérios
        if (bestBlocks.Count > 0)
        {
            // retornar uma posição aleatória entre os melhores blocos encontrados
            return bestBlocks[Random.Range(0, bestBlocks.Count)];
        }

        return null;
    }

    /// <summary>
    /// Retorna uma posição aleatória dentro de uma área livre na parede 
    /// que acomode o tamanho necessário do puzzle, sem considerar uma altura alvo
    /// </summary>
    /// <param name="anchor">Âncora da parede</param>
    /// <param name="requiredSize">Tamanho necessário do puzzle</param>
    /// <returns>Posição aleatória dentro de uma área livre</returns>
    public Vector3? GetRandomSufficientZone(MRUKAnchor anchor, Vector2 requiredSize)
    {
        // se a âncora não estiver no dicionário de grelhas, retornar nulo
        if (!wallGrids.TryGetValue(anchor, out WallGrid grid)) return null;
        // se a grelha não tiver células, retornar nulo
        if (grid.Rows == 0 || grid.Columns == 0) return null;

        // calcular o número de colunas e linhas necessárias para acomodar o tamanho do puzzle
        int requiredCols = Mathf.Max(1, Mathf.CeilToInt(requiredSize.x / grid.CellSize));
        int requiredRows = Mathf.Max(1, Mathf.CeilToInt(requiredSize.y / grid.CellSize));

        // lista para armazenar os centros das áreas livres que atendem aos critérios
        List<Vector3> validBlockCenters = new List<Vector3>();

        // iterar sobre todas as células possíveis na grelha que podem acomodar o tamanho do puzzle
        for (int x = 0; x <= grid.Columns - requiredCols; x++)
        {
            for (int y = 0; y <= grid.Rows - requiredRows; y++)
            {
                // verificar se a área retangular na grelha está livre
                if (IsAreaFree(grid, x, y, requiredCols, requiredRows))
                {
                    // calcular a posição central da área livre em coordenadas world space
                    Vector3 startCellPos = grid.Cells[x, y].worldPosition;
                    Vector3 endCellPos = grid.Cells[x + requiredCols - 1, y + requiredRows - 1].worldPosition;
                    Vector3 centerPos = (startCellPos + endCellPos) / 2f;
                    // adicionar a posição central à lista de centros de blocos válidos
                    validBlockCenters.Add(centerPos);
                }
            }
        }

        // se houver blocos válidos que atendem aos critérios
        if (validBlockCenters.Count > 0)
        {
            // retornar uma posição aleatória entre os centros de blocos válidos encontrados
            return validBlockCenters[Random.Range(0, validBlockCenters.Count)];
        }

        return null;
    }

    /// <summary>
    /// Marca uma área retangular na grelha como ocupada, com base na posição central global, 
    /// tamanho e margem de segurança fornecidos
    /// </summary>
    /// <param name="anchor">Âncora da parede</param>
    /// <param name="centerWorldPos">Posição central em coordenadas world space</param>
    /// <param name="size">Tamanho da área a marcar como ocupada</param>
    /// <param name="padding">Margem de segurança ao redor da área</param>
    public void MarkAreaAsOccupied(MRUKAnchor anchor, Vector3 centerWorldPos, Vector2 size, float padding)
    {
        // se a âncora não estiver no dicionário de grelhas, não há nada a marcar
        if (!wallGrids.TryGetValue(anchor, out WallGrid grid)) return;

        // converter a posição central global para coordenadas locais da âncora
        Vector3 localCenter = anchor.transform.InverseTransformPoint(centerWorldPos);

        // calcular a metade da largura e altura da área, incluindo a margem de segurança
        float halfWidth = (size.x / 2f) + padding;
        float halfHeight = (size.y / 2f) + padding;

        // criar um retângulo representando a área ocupada em coordenadas locais
        Rect occupiedRect = new Rect(localCenter.x - halfWidth, localCenter.y - halfHeight, halfWidth * 2f, halfHeight * 2f);

        // iterar sobre todas as células da grelha
        for (int x = 0; x < grid.Columns; x++)
        {
            for (int y = 0; y < grid.Rows; y++)
            {
                if (!grid.Cells[x, y].isFree) continue; // se a célula já estiver ocupada, passa para a próxima

                // se a posição local da célula estiver dentro do retângulo ocupado
                if (occupiedRect.Contains(grid.Cells[x, y].localPosition))
                {
                    grid.Cells[x, y].isFree = false; // marcar a célula como ocupada
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos || wallGrids == null) return;

        Vector3 cubeSize = new Vector3(gridCellResolution * 0.85f, gridCellResolution * 0.85f, 0.015f);

        foreach (var grid in wallGrids.Values)
        {
            if (grid.Cells == null) continue;

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    GridCell cell = grid.Cells[x, y];

                    Gizmos.color = cell.isFree ? freeCellColor : occupiedCellColor;

                    // Alinhar os quadrados com a rotação da parede para ficarem rasos
                    Matrix4x4 rotationMatrix = Matrix4x4.TRS(cell.worldPosition, grid.ParentAnchor.transform.rotation, Vector3.one);
                    Gizmos.matrix = rotationMatrix;
                    
                    Gizmos.DrawCube(Vector3.zero, cubeSize);
                }
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
#endregion