using UnityEngine;

#region #my_code
public class LidHinge : MonoBehaviour
{
    [SerializeField] private float openAngle = -90f; // ângulo quando a tampa está aberta
    [SerializeField] private float closedAngle = 0f; // ângulo quando a tampa está fechada
    [SerializeField] private float speed = 90f; // velocidade de rotação da tampa

    private float targetAngle; // ângulo alvo para a rotação da tampa
    private bool isOpen = false; // estado atual da tampa (aberta ou fechada)

    /// <summary>
    /// Inicializa a tampa com o ângulo fechado
    /// </summary>
    void Start()
    {
        targetAngle = closedAngle;
    }

    /// <summary>
    /// Atualiza a rotação da tampa a cada frame, 
    /// movendo-a em direção ao ângulo alvo
    /// </summary>
    void Update()
    {
        // calcula o ângulo atual da tampa em relação ao eixo de rotação
        float current = transform.localEulerAngles.x;
        // ajusta o ângulo para estar no intervalo [-180, 180]
        if (current > 180f) current -= 360f;

        // move o ângulo atual de forma suave em direção ao ângulo alvo usando MoveTowardsAngle
        float next = Mathf.MoveTowardsAngle(current, targetAngle, speed * Time.deltaTime);
        // aplica a nova rotação à tampa
        transform.localEulerAngles = new Vector3(next, 0f, 0f);
    }

    /// <summary>
    /// Abre a tampa, definindo o estado como aberto 
    /// e o ângulo alvo como o ângulo de abertura
    /// </summary>
    public void Open()
    {
        isOpen = true;
        targetAngle = openAngle;
    }

    /// <summary>
    /// Fecha a tampa, definindo o estado como fechado 
    /// e o ângulo alvo como o ângulo de fechamento
    /// </summary>
    public void Close()
    {
        isOpen = false;
        targetAngle = closedAngle;
    }

    /// <summary>
    /// Retorna verdadeiro se a tampa estiver aberta, falso caso contrário
    /// </summary>
    public bool IsOpen => isOpen;
}
#endregion