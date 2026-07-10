using UnityEngine;
using Meta.XR.MRUtilityKit;

public abstract class BasePuzzle : MonoBehaviour
{
    public string puzzleId; // ID único (ex: "couch_simon_says")
    public bool isSolved = false; // flag para indicar se o puzzle já foi resolvido

    /// <summary>
    /// Posição do dígito do finalCode que este puzzle revela
    /// 
    /// Atribuído automaticamente pelo EscapeRoomManager após o spawn
    /// 
    /// -1 significa "ainda não atribuído"
    /// </summary>
    [HideInInspector] public int assignedCodePosition = -1;

    // evento que é disparado quando o puzzle é resolvido
    public System.Action<BasePuzzle> OnPuzzleSolved;

    // cada puzzle implementa a sua própria lógica
    public abstract void Initialize(MRUKAnchor anchor);
    public abstract void Activate(); 

    /// <summary>
    /// Chamado pelo EscapeRoomManager após atribuir o assignedCodePosition
    /// 
    /// Puzzles que precisam do dígito do código devem fazer override deste método
    /// </summary>
    /// <param name="digit">O dígito do código a ser configurado</param>
    public virtual void SetupCodeDigit(int digit) { }

    protected void CompletePuzzle()
    {
        isSolved = true;
        OnPuzzleSolved?.Invoke(this);
        Debug.Log($"[EscapeRoom] Puzzle '{puzzleId}' SOLVED!");
    }
}
