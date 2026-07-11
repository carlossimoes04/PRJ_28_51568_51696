using UnityEngine;
using System.Collections;

#region #my_code
public class UISpawnAtCamera : MonoBehaviour
{
    [Tooltip("câmara do jogador")]
    public Transform playerCamera;

    [Tooltip("a distância inicial a que o painel aparece")]
    public float distance = 0.8f;

    /// <summary>
    /// Chamado quando o script é iniciado
    /// 
    /// Se a câmara do jogador não estiver definida, 
    /// tenta usar a câmara principal
    /// </summary>
    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // inicia a corrotina para posicionar o painel 
        // depois de a câmara do jogador estar pronta
        StartCoroutine(SpawnAfterTrackingStarts());
    }

    private IEnumerator SpawnAfterTrackingStarts()
    {
        // espera 0.5 segundos
        // esta corrotina garante que o painel só aparece depois de a câmara 
        // do jogador estar pronta, evitando que apareça no chão ou em posições 
        // estranhas
        yield return new WaitForSeconds(0.5f);

        if (playerCamera != null)
        {
            // descobre a direção em frente na horizontal
            Vector3 forwardXZ = playerCamera.forward;
            forwardXZ.y = 0;
            if (forwardXZ == Vector3.zero) forwardXZ = playerCamera.up;
            forwardXZ.Normalize();

            // posiciona o objeto à altura real dos olhos
            transform.position = playerCamera.position + (forwardXZ * distance);
            
            // vira o painel para o jogador
            Vector3 lookDirection = transform.position - playerCamera.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }
}
#endregion