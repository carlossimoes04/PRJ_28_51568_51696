using UnityEngine;

/// <summary>
/// Classe que representa uma tomada de entrada para cabos de Morse Code
/// </summary>
public class InputPort : MonoBehaviour
{
    [Header("Configuração da Tomada")]
    [Tooltip("ID desta tomada de entrada (ex: '1', '2', '3')")]
    public string portId;

    [Tooltip("Transform que define onde a ponta do cabo irá fazer snap")]
    public Transform snapAnchor;

    [HideInInspector]
    public bool isOccupied = false;  // indica se o input port já tem um cabo conectado

    void Awake()
    {
        // se não se definir um ponto de snap específico, usa a posição do próprio objeto
        if (snapAnchor == null)
        {
            snapAnchor = transform;
        }
    }
}