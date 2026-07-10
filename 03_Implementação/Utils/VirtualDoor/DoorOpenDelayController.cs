using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DoorOpenDelayController : MonoBehaviour
{
    [SerializeField] private Animator animator; // animator da porta
    [SerializeField] private float delaySeconds = 3f; // delay após acesso permitido para abrir a porta
    [SerializeField] private string openTriggerName = "Open"; // nome do trigger no Animator para abrir a porta
    
    [Header("Eventos")]
    [SerializeField] private UnityEvent onDoorOpened = new UnityEvent();
    public UnityEvent OnDoorOpenedEvent => onDoorOpened;

    private bool isOpening; // flag para saber se a porta já está em processo de abertura

    public void OpenAfterDelay() // método público para iniciar o processo de abertura com delay
    {
        if (isOpening) return;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine() // coroutine que gerencia o delay e a abertura da porta
    {
        isOpening = true;
        yield return new WaitForSeconds(delaySeconds);
        animator.SetTrigger(openTriggerName);
        onDoorOpened?.Invoke(); // chama o evento de porta aberta
    }
}