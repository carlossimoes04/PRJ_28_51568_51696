using UnityEngine;

public class LidUnlockController : MonoBehaviour
{
    [SerializeField] private KeyTab leftKeyTab; // referência à KeyTab da esquerda
    [SerializeField] private KeyTab rightKeyTab; // referência à KeyTab da direita
    [SerializeField] private LidHinge lidHinge; // referência ao componente LidHinge que controla a tampa
    // tempo de espera após ambas as KeyTabs serem desbloqueadas antes de abrir a tampa
    [SerializeField] private float delayAfterUnlock = 1f; 
    // tempo de espera após ambas as KeyTabs serem bloqueadas antes de fechar a tampa
    [SerializeField] private float delayAfterLock = 1f;

    // enum para representar os estados da tampa
    private enum LidState { Closed, WaitingToOpen, Open, WaitingToClose }
    // estado atual da tampa
    private LidState state = LidState.Closed;
    // temporizador para controlar os atrasos entre os estados
    private float timer = 0f;

    /// <summary>
    /// Atualiza o estado da tampa a cada frame, verificando o estado 
    /// das KeyTabs e controlando a abertura/fecho da tampa com 
    /// base nos atrasos definidos
    /// </summary>
    void Update()
    {
        // verifica se ambas as KeyTabs estão desbloqueadas e se ambas estão bloqueadas
        bool bothUnlocked = leftKeyTab.IsUnlocked() && rightKeyTab.IsUnlocked();
        bool bothLocked = !leftKeyTab.IsUnlocked() && !rightKeyTab.IsUnlocked();
        // verifica se ambas as animações das KeyTabs estão completas
        bool animationsDone = leftKeyTab.IsAnimationComplete() && rightKeyTab.IsAnimationComplete();

        switch (state) // verifica o estado atual da tampa
        {
            case LidState.Closed: // se a tampa estiver fechada
                // espera que ambas as KeyTabs estejam desbloqueadas 
                // e que ambas as animações estejam completas
                if (bothUnlocked && animationsDone)
                {
                    state = LidState.WaitingToOpen;
                    timer = delayAfterUnlock;
                }
                break;

            case LidState.WaitingToOpen: // se a tampa estiver à espera para abrir
                // se entretanto uma KeyTab for bloqueada, cancela a abertura
                if (!bothUnlocked)
                {
                    state = LidState.Closed;
                    break;
                }
                // decrementa o temporizador e abre a tampa quando o tempo expira
                timer -= Time.deltaTime;
                // quando o temporizador expira, abre a tampa e muda o estado para aberto
                if (timer <= 0f) 
                {
                    lidHinge.Open(); // abre a tampa
                    state = LidState.Open; // muda o estado para aberto
                }
                break;

            case LidState.Open: // se a tampa estiver aberta
                // espera que ambas as KeyTabs estejam bloqueadas 
                // e que ambas as animações estejam completas
                if (bothLocked && animationsDone)
                {
                    state = LidState.WaitingToClose;
                    timer = delayAfterLock;
                }
                break;

            case LidState.WaitingToClose: // se a tampa estiver à espera para fechar
                // se entretanto uma KeyTab for desbloqueada, cancela o fecho
                if (!bothLocked)
                {
                    state = LidState.Open;
                    break;
                }
                // decrementa o temporizador e fecha a tampa quando o tempo expira
                timer -= Time.deltaTime;
                // quando o temporizador expira, fecha a tampa e muda o estado para fechado
                if (timer <= 0f)
                {
                    lidHinge.Close();
                    state = LidState.Closed;
                }
                break;
        }
    }
}