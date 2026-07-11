using UnityEngine;
using Meta.XR.MRUtilityKit;

#region #ai_assisted
/// <summary>
/// Classe que representa uma grelha de células para análise de paredes
/// </summary>
public class WallGrid
{
    /// <summary>
    /// Âncora pai associada a esta grelha
    /// </summary>
    public MRUKAnchor ParentAnchor { get; private set; }
    /// <summary>
    /// Tamanho de cada célula na grelha
    /// </summary>
    public float CellSize { get; private set; }
    /// <summary>
    /// Matriz de células que compõem a grelha
    /// </summary>
    public GridCell[,] Cells { get; private set; }
    /// <summary>
    /// Número de colunas na grelha
    /// </summary>
    public int Columns { get; private set; }
    /// <summary>
    /// Número de linhas na grelha
    /// </summary>
    public int Rows { get; private set; }

    /// <summary>
    /// Inicializa uma nova instância da classe WallGrid com a 
    /// âncora pai e o tamanho da célula especificados
    /// </summary>
    /// <param name="anchor">Âncora pai</param>
    /// <param name="cellSize">Tamanho da célula</param>
    public WallGrid(MRUKAnchor anchor, float cellSize)
    {
        ParentAnchor = anchor; // define a âncora pai
        CellSize = cellSize; // define o tamanho da célula
        GenerateGrid(); // gera a grelha de células com base na âncora e no tamanho da célula
    }

    /// <summary>
    /// Gera a grelha de células
    /// </summary>
    private void GenerateGrid()
    {
        // verifica se a âncora pai possui um retângulo de plano válido
        // se não houver, a função retorna sem gerar a grelha
        if (!ParentAnchor.PlaneRect.HasValue) return;

        // obtém o retângulo do plano da âncora pai
        Rect rect = ParentAnchor.PlaneRect.Value;
        // calcula o número de colunas e linhas com base no 
        // tamanho do retângulo e no tamanho da célula
        Columns = Mathf.CeilToInt(rect.width / CellSize);
        Rows = Mathf.CeilToInt(rect.height / CellSize);

        // inicializa a matriz de células com o número de colunas e linhas calculado
        Cells = new GridCell[Columns, Rows];

        // calcula a posição inicial (canto inferior esquerdo) da primeira célula
        float startX = rect.xMin + CellSize / 2f;
        float startY = rect.yMin + CellSize / 2f;

        // itera sobre todas as colunas e linhas
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                // calcula a posição local da célula com base na posição inicial e no tamanho da célula
                Vector2 localPos = new Vector2(startX + x * CellSize, startY + y * CellSize);
                // converte a posição local para posição global usando a transformação da âncora pai
                Vector3 worldPos = ParentAnchor.transform.TransformPoint(localPos);
                /*
                localPos -> posição da célula em relação à grelha da parede (local space)
                worldPos -> posição da célula na sala real (world space)

                foi necessário passar de local space para world space, pois, para testar se uma célula
                está livre, é preciso saber onde ela fica na sala real para depois ver se bate 
                em algum obstáculo
                */

                // cria uma nova célula na posição (x, y) da matriz de células
                Cells[x, y] = new GridCell
                {
                    localPosition = localPos, // posição da célula em relação à grelha da parede
                    worldPosition = worldPos, // posição da célula na sala real
                    isFree = true // inicialmente, todas as células são consideradas livres
                };
            }
        }
    }
}
#endregion