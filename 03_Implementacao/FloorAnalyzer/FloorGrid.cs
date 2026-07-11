using UnityEngine;
using Meta.XR.MRUtilityKit;

#region #ai_assisted

/// <summary>
/// Classe que representa uma grelha de células no chão, associada a uma âncora de chão específica
/// </summary>
public class FloorGrid
{
    // a âncora de chão à qual esta grelha está associada
    public MRUKAnchor ParentAnchor { get; private set; }
    // o tamanho de cada célula da grelha em metros
    public float CellSize { get; private set; }
    // matriz 2D de células que compõem a grelha, cada célula representa uma área do chão
    public FloorGridCell[,] Cells { get; private set; }
    // número de colunas da grelha
    public int Columns { get; private set; }
    // número de linhas da grelha
    public int Rows { get; private set; }

    /// <summary>
    /// Construtor da classe FloorGrid, que inicializa a grelha com base na âncora 
    /// de chão fornecida e no tamanho das células
    /// </summary>
    /// <param name="anchor">A âncora de chão à qual a grelha está associada</param>
    /// <param name="cellSize">O tamanho de cada célula da grelha em metros</param>
    public FloorGrid(MRUKAnchor anchor, float cellSize)
    {
        ParentAnchor = anchor; // atribui a âncora de chão a ParentAnchor
        CellSize = cellSize; // atribui o tamanho das células a CellSize

        // chama o método para gerar a grelha de células com base na âncora 
        // e no tamanho das células
        GenerateGrid(); 
    }

    /// <summary>
    /// Este método gera a grelha de células com base na âncora de chão e no tamanho das células
    /// </summary>
    private void GenerateGrid()
    {
        // verifica se a âncora de chão possui um retângulo de plano definido
        if (!ParentAnchor.PlaneRect.HasValue) return;

        // obtém o retângulo de plano da âncora de chão
        Rect rect = ParentAnchor.PlaneRect.Value;
        // calcula o número de colunas com base no tamanho do retângulo e no tamanho das células
        Columns = Mathf.CeilToInt(rect.width / CellSize);
        // calcula o número de linhas com base no tamanho do retângulo e no tamanho das células
        Rows = Mathf.CeilToInt(rect.height / CellSize);

        // inicializa a matriz de células com o número de colunas e linhas calculado
        Cells = new FloorGridCell[Columns, Rows];

        // calcula a posição inicial para a primeira célula, 
        // garantindo que as células fiquem centradas no retângulo
        float startX = rect.xMin + CellSize / 2f;
        float startY = rect.yMin + CellSize / 2f;

        // percorre todas as colunas e linhas para criar cada célula da grelha
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                // calcula a posição local da célula com base na posição inicial e no tamanho das células
                Vector2 localPos = new Vector2(startX + x * CellSize, startY + y * CellSize);
                // converte a posição local da célula para coordenadas de mundo usando a transformação 
                // da âncora de chão
                Vector3 worldPos = ParentAnchor.transform.TransformPoint(localPos);
                /*
                localPos -> posição da célula em relação à grelha do chão (local space)
                worldPos -> posição da célula na sala real (world space)

                foi necessário passar de local space para world space, pois, para testar se uma célula
                está livre, é preciso saber onde ela fica na sala real para depois ver se bate 
                em algum obstáculo
                */

                // inicializa a célula na matriz com a posição local, posição de mundo 
                // e marca como livre (isFree = true)
                Cells[x, y] = new FloorGridCell
                {
                    localPosition = localPos,
                    worldPosition = worldPos,
                    isFree = true 
                };
            }
        }
    }
}
#endregion