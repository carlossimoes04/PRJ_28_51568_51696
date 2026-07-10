// DebugTouch.cs
using UnityEngine;

public class DebugTouch : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Keypad | trigger: {other.name} tocou em {gameObject.name}");
    }
}