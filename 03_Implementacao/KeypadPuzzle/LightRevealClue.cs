using UnityEngine;

/// <summary>
/// Classe que revela uma pista quando iluminada por uma luz com a tag "FlashlightLight"
/// </summary>
public class LightRevealClue : MonoBehaviour
{
    private Light spotLight; // referência à luz que ilumina a pista
    private Renderer _renderer; // referência ao componente Renderer do objeto que contém a pista

    // MaterialPropertyBlock usado para alterar a cor do material sem criar instâncias adicionais
    private MaterialPropertyBlock _propBlock;
    private float _currentAlpha = 0f; // valor atual da opacidade do material
    public float fadeSpeed = 3f; // velocidade de transição da opacidade

    /// <summary>
    /// Inicializa o componente, procurando a luz com a tag "FlashlightLight" 
    /// e configurando a opacidade inicial
    /// </summary>
    void Start()
    {
        _renderer = GetComponent<Renderer>(); // obtém o componente Renderer do objeto

        // cria um MaterialPropertyBlock para mexer com as propriedades 
        // do material sem instanciar novos materiais
        _propBlock = new MaterialPropertyBlock();

        // procura todas as luzes na cena, incluindo as inativas
        Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // itera sobre todas as luzes encontradas
        foreach (Light light in allLights)
        {
            // se a luz tiver a tag "FlashlightLight"
            if (light.CompareTag("FlashlightLight"))
            {
                spotLight = light; // armazena a referência à luz encontrada
                break;
            }
        }

        if (spotLight == null) // se nenhuma luz com a tag "FlashlightLight" foi encontrada
            Debug.LogWarning("LightRevealClue: nenhuma luz com tag FlashlightLight encontrada");

        SetAlpha(0f); // define a opacidade inicial do material como 0 (invisível)
    }

    /// <summary>
    /// Método chamado a cada frame para atualizar a opacidade 
    /// do material com base na iluminação
    /// </summary>
    void Update()
    {
        if (spotLight == null) return; // se a luz não foi encontrada, não faz nada

        // se a luz não está ativa ou não está habilitada
        if (!spotLight.enabled || !spotLight.gameObject.activeInHierarchy)
        {
            // faz a transição da opacidade atual para 0 (invisível) usando Lerp
            _currentAlpha = Mathf.Lerp(_currentAlpha, 0f, Time.deltaTime * fadeSpeed);
            // aplica a opacidade atual ao material
            SetAlpha(_currentAlpha);
            // retorna para não executar o restante do código
            return;
        }

        // verifica se o objeto está iluminado pela luz
        bool isLit = IsIlluminated();
        // define o alvo da opacidade com base na iluminação (1 se iluminado, 0 se não)
        float target = isLit ? 1f : 0f;
        // faz a transição da opacidade atual para o alvo usando Lerp
        _currentAlpha = Mathf.Lerp(_currentAlpha, target, Time.deltaTime * fadeSpeed);
        // aplica a opacidade atual ao material
        SetAlpha(_currentAlpha);
    }

    /// <summary>
    /// Verifica se o objeto está iluminado pela luz com a tag "FlashlightLight"
    /// </summary>
    /// <returns></returns>
    bool IsIlluminated()
    {   
        // calcula a direção do objeto em relação à luz
        Vector3 toObject = transform.position - spotLight.transform.position;
        float distance = toObject.magnitude;

        // se a distância do objeto à luz for maior que o alcance da luz, retorna falso
        if (distance > spotLight.range) return false;

        // calcula o ângulo entre a direção da luz e a direção para o objeto
        float angle = Vector3.Angle(spotLight.transform.forward, toObject);
        // se o ângulo for maior que metade do ângulo do spot da luz, retorna falso
        if (angle > spotLight.spotAngle / 2f) return false;

        return true;
    }

    /// <summary>
    /// Define a opacidade do material usando MaterialPropertyBlock
    /// </summary>
    /// <param name="alpha">Opacidade</param>
    void SetAlpha(float alpha)
    {
        // obtém o MaterialPropertyBlock atual do Renderer
        _renderer.GetPropertyBlock(_propBlock);
        // define a cor base do material com a opacidade especificada
        _propBlock.SetColor("_BaseColor", new Color(1f, 1f, 1f, alpha));
        // aplica o MaterialPropertyBlock atualizado ao Renderer
        _renderer.SetPropertyBlock(_propBlock);
    }
}