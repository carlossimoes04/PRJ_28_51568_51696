using UnityEngine;

#region #my_code
public class DebugTouch : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Keypad | trigger: {other.name} tocou em {gameObject.name}");
    }
}
#endregion