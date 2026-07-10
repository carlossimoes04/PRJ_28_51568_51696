using UnityEngine;
using Meta.XR.MRUtilityKit;

public class VirtualDoorSpawner : MonoBehaviour
{
    [Header("Configuração")]
    public GameObject doorPrefab; // referência ao prefab da porta virtual a ser instanciada

    [Header("Ajuste de Tamanho: Largura e Altura Extra")]
    public float extraWidth = 0.1f; // aumenta a largura da porta em 10 cm para garantir que cobre toda a moldura da porta real
    public float extraHeight = 0.1f; // aumenta a altura da porta em 10 cm para garantir que cobre toda a moldura da porta real

    [Header("Ajuste de Posição")]
    [Tooltip("Move a porta para dentro da sala. Valores típicos: 0.01 a 0.05 (1 a 5 cm)")]
    public float depthOffset = 0.01f; // move a porta para dentro da sala para evitar clipping com a malha da parede

    /// <summary>
    /// Faz spawn da porta virtual na posição correta, com a rotação correta e ajustada para cobrir 
    /// a moldura da porta real
    /// <br/>
    /// <br/> A porta é instanciada como filha do anchor da moldura da porta, garantindo que se mova 
    /// corretamente com o ambiente
    /// </summary>
    /// <param name="room">Sala do jogador</param>
    public void SpawnVirtualDoor(MRUKRoom room)
    {
        foreach (var anchor in room.Anchors) // para cada anchor da sala
        {
            // se o anchor tiver a label "DOOR_FRAME", ou seja, se for um anchor de moldura de porta
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.DOOR_FRAME))
            {
                if (anchor.PlaneRect.HasValue) // se o anchor tiver uma área retangular (o que é esperado para uma moldura de porta)
                {
                    Rect rect = anchor.PlaneRect.Value; // obtém as dimensões da moldura da porta a partir do anchor
                    float targetWidth = rect.width + extraWidth; // largura alvo da porta virtual, ajustada para cobrir a moldura real
                    float targetHeight = rect.height + extraHeight; // altura alvo da porta virtual, ajustada para cobrir a moldura real

                    // instancia na origem sem rotação para medir a malha 3D real
                    GameObject vDoor = Instantiate(doorPrefab, Vector3.zero, Quaternion.identity);

                    // variáveis para armazenar as dimensões da malha 3D da porta virtual 
                    // (valores iguais a 1f por padrão para evitar problemas de escala zero)
                    float sizeX = 1f, sizeY = 1f, sizeZ = 1f;

                    // medição da Bounding Box (caixa delimitadora) da malha 3D da porta virtual 
                    // para descobrir a sua largura, altura e espessura reais
                    Renderer[] renderers = vDoor.GetComponentsInChildren<Renderer>(); // obtém todos os renderers da porta virtual (incluindo filhos) para calcular a bounding box total
                    if (renderers.Length > 0) // se houver renderers
                    {
                        Bounds bounds = renderers[0].bounds; // inicia os bounds com o primeiro renderer
                        foreach (Renderer r in renderers) // para cada renderer
                        {
                            // expande os bounds para incluir o renderer atual, resultando numa 
                            // bounding box total que envolve toda a malha 3D da porta virtual
                            bounds.Encapsulate(r.bounds); 
                        }

                        sizeX = bounds.size.x; // largura real da malha 3D da porta virtual
                        sizeY = bounds.size.y; // altura real da malha 3D da porta virtual
                        sizeZ = bounds.size.z; // espessura real da malha 3D da porta virtual
                    }

                    // garante que as dimensões não sejam zero para evitar problemas de escala
                    if (sizeX <= 0.01f) sizeX = 1f;
                    if (sizeY <= 0.01f) sizeY = 1f;
                    if (sizeZ <= 0.01f) sizeZ = 1f;

                    // calcula a escala necessária para ajustar a malha 3D da porta virtual às 
                    // dimensões alvo, mantendo a proporção correta
                    float scaleX = 1f;
                    float scaleZ = 1f;
                    float scaleY = targetHeight / sizeY; // a altura é sempre o Y

                    // percebe para que lado a porta foi modelada
                    if (sizeX >= sizeZ)
                    {
                        // o modelo 3D é mais largo no X (foi modelado de frente)
                        scaleX = targetWidth / sizeX; 
                        scaleZ = 1f; // a espessura fica normal
                    }
                    else
                    {
                        // o modelo 3D é mais largo no Z (foi modelado de lado)
                        scaleZ = targetWidth / sizeZ; 
                        scaleX = 1f; // a espessura fica normal
                    }

                    // aplica a escala calculada à porta virtual para que fique do tamanho certo para 
                    // cobrir a moldura da porta real
                    vDoor.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                    // torna a porta virtual filha do anchor da moldura da porta
                    // isto garante que a porta virtual mantenha a posição e rotação 
                    // corretas em relação à moldura da porta real, mesmo que o jogador 
                    // se mova ou que haja pequenas imprecisões no reconhecimento do ambiente
                    vDoor.transform.SetParent(anchor.transform, false);
                    vDoor.transform.localPosition = new Vector3(0, 0, depthOffset);
                    vDoor.transform.localRotation = doorPrefab.transform.localRotation;

                    Debug.Log($"[EscapeRoom] porta criada: {vDoor.name}"); // debug

                    break; // para evitar criar múltiplas portas virtuais se houver mais de um anchor de moldura de porta
                }
            }
        }
    }
}