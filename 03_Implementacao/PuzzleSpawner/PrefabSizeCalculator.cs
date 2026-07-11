using UnityEngine;

#region #my_code
/// <summary>
/// Calcula o tamanho de um prefab com base nos seus renderers e rect transforms
/// 
/// Se o prefab não tiver renderers ou rect transforms, retorna um tamanho padrão de (0.2, 0.2)
/// </summary>
public static class PrefabSizeCalculator
{
    public static Vector2 Calculate(GameObject prefab)
    {
        if (prefab == null) return new Vector2(0.2f, 0.2f); // tamanho padrão para prefabs nulos
 
        // instancia o prefab numa posição fora da tela para evitar interferência visual
        GameObject instantiatedPrefab = Object.Instantiate(prefab, new Vector3(0, -9999f, 0), prefab.transform.rotation);
        
        // obtém todos os renderers e rect transforms do prefab instanciado
        Renderer[] renderers = instantiatedPrefab.GetComponentsInChildren<Renderer>();
        RectTransform[] rectTransforms = instantiatedPrefab.GetComponentsInChildren<RectTransform>();

        /*
        os renderers são as componentes de renderização de todos os objetos 3D de um prefab
        (como o MeshRenderer) e os rect transforms são as componentes de layout de todos os 
        objetos UI de um prefab

        neste caso, só se procuram os renderers e rect transforms dos objetos filhos do prefab 
        instanciado
        */

        // se não houver renderers ou rect transforms, retorna o tamanho padrão
        if (renderers.Length == 0 && rectTransforms.Length == 0)
        {
            Object.DestroyImmediate(instantiatedPrefab); // limpa o prefab da cena
            return new Vector2(0.2f, 0.2f); // tamanho padrão
        }

        // calcula os bounds que encapsulam todos os renderers e rect transforms
        Bounds bounds = new Bounds(instantiatedPrefab.transform.position, Vector3.zero);
        bool hasBounds = false; // flag para indicar se há bounds iniciais válidos

        /*
        as bounds são as bounding boxes que envolvem os renderers de cada objeto do prefab
        */

        if (renderers.Length > 0) // se houver renderers
        {
            bounds = renderers[0].bounds; // inicia os bounds com o primeiro renderer
            hasBounds = true; // marca a flag como verdadeira, pois há bounds válidos
            for (int i = 1; i < renderers.Length; i++) // por cada renderer restante
            {
                bounds.Encapsulate(renderers[i].bounds); // encapsula os bounds do renderer atual
            }
        }

        /*
        através do método Encapsulate, o código junta iterativamente todas estas caixas individuais 
        numa única Bounding Box principal
        
        isto garante que o cálculo final representa o volume 3D total (largura, altura, profundidade) 
        e todas as peças que compõem o prefab inteiro, e não apenas de uma peça isolada
        */

        if (rectTransforms.Length > 0) // se existirem rect transforms
        {
            Vector3[] corners = new Vector3[4]; // array para guardar os cantos de cada rect transform
            foreach (var rt in rectTransforms) // para cada rect transform
            {
                // converte os limites 2D locais do elemento de UI (RectTransform) em coordenadas 
                // geométricas tridimensionais (World Space) e guarda-as no array 'corners'
                rt.GetWorldCorners(corners); 
                for (int i = 0; i < 4; i++) // para cada canto do rect transform
                {
                    if (!hasBounds) // se ainda não houver bounds válidos
                    {
                        // inicia os bounds com o primeiro canto do rect transform
                        bounds = new Bounds(corners[i], Vector3.zero);
                        hasBounds = true; // marca a flag como verdadeira
                    }
                    else // caso contrário, encapsula o canto atual nos bounds existentes
                    {
                        bounds.Encapsulate(corners[i]);
                    }
                }
            }
        }

        Object.DestroyImmediate(instantiatedPrefab); // limpa o prefab da cena

        // retorna o tamanho dos bounds, garantindo que não seja menor que um valor mínimo 
        // para evitar problemas de escala
        return new Vector2(Mathf.Max(0.01f, bounds.size.x), Mathf.Max(0.01f, bounds.size.y));
    }
}
#endregion