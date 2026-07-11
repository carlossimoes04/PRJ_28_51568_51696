using UnityEngine;

#region #ai_assisted

/// <summary>
/// Representa uma célula da grelha do chão, contendo informações 
/// sobre a sua posição e se está livre ou ocupada
/// 
/// É "struct" porque é um tipo de pequeno e simples, pois só guarda dados de posição e estado, 
/// e não precisa de comportamento complexo
/// </summary>
public struct FloorGridCell
{
    // posição da célula em world space
    public Vector3 worldPosition;
    // posição da célula em local space
    public Vector2 localPosition;
    // indica se a célula está livre
    public bool isFree;
}
#endregion