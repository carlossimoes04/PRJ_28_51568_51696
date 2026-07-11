using UnityEngine;

#region #my_code
public class KeyTab : MonoBehaviour
{
    public float lockedAngle = 0f; // ângulo quando a aba está trancada
    public float unlockedAngle = -90f; // ângulo quando a aba está destrancada
    public float rotationSpeed = 180f; // velocidade de rotação
    public Vector3 rotationAxis = Vector3.right; // muda para up ou forward se necessário

    private bool isUnlocked = false; // estado atual da aba (trancada ou destrancada)
    private float targetAngle; // ângulo alvo para a rotação

    /// <summary>
    /// Inicializa a aba com o ângulo trancado
    /// </summary>
    void Start()
    {
        targetAngle = lockedAngle;
    }

    /// <summary>
    /// Atualiza a rotação da aba a cada frame, 
    /// movendo-a em direção ao ângulo alvo
    /// </summary>
    void Update()
    {
        // calcula o ângulo atual da aba em relação ao eixo de rotação
        float currentAngle = Vector3.Dot(transform.localEulerAngles, rotationAxis);
        
        // ajusta o ângulo para estar no intervalo [-180, 180] 
        // para evitar erros na rotação
        if (currentAngle > 180f) currentAngle -= 360f;
        // move o ângulo atual de forma suave em direção ao ângulo alvo usando MoveTowardsAngle
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        
        // aplica a nova rotação à aba
        transform.localEulerAngles = rotationAxis * newAngle;
    }

    /// <summary>
    /// Alterna o estado da aba entre trancada e destrancada quando é "poked"
    /// </summary>
    public void OnPoked()
    {
        isUnlocked = !isUnlocked; // inverte o estado da aba
        // define o ângulo alvo com base no novo estado
        targetAngle = isUnlocked ? unlockedAngle : lockedAngle; 
    }

    public bool IsUnlocked() => isUnlocked; // retorna o estado atual da aba (trancada ou destrancada)

    /// <summary>
    /// Verifica se a animação de rotação da aba foi concluída,
    /// ou seja, se a aba atingiu o ângulo alvo
    /// </summary>
    /// <returns></returns>
    public bool IsAnimationComplete()
    {
        // calcula o ângulo atual da aba em relação ao eixo de rotação
        float currentAngle = Vector3.Dot(transform.localEulerAngles, rotationAxis);
        // ajusta o ângulo para estar no intervalo [-180, 180]
        if (currentAngle > 180f) currentAngle -= 360f;
        // verifica se a diferença entre o ângulo atual e o 
        // ângulo alvo é menor que um pequeno valor (0.5 graus)
        return Mathf.Abs(currentAngle - targetAngle) < 0.5f;
    }
}
#endregion