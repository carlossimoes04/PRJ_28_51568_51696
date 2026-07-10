using UnityEngine;

/// <summary>
/// Representa uma célula individual na grelha de análise da parede
/// 
/// É "struct" porque é um tipo de pequeno e simples, pois só guarda dados de posição e estado, 
/// e não precisa de comportamento complexo
/// </summary>
public struct GridCell
{
    public Vector3 worldPosition; // posição global da célula no espaço
    public Vector2 localPosition; // posição local da célula em relação à parede
    public bool isFree; // indica se a célula está livre (true) ou ocupada (false)
}
