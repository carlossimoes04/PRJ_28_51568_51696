using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace NavKeypad
{
    /// <summary>
    /// Classe que representa um keypad com código de acesso
    /// 
    /// Este script pertence ao asset do Keypad que se arranjou na Unity Asset Store,
    /// no entanto, foi minimamente modificado para se adaptar ao projeto
    /// </summary>
    public class Keypad : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField] private UnityEvent onAccessGranted;
        [SerializeField] private UnityEvent onAccessDenied;
        
        [Header("Escape Room")]
        [Tooltip("Se ativo, ignora o Keypad Combo e usa o código final do EscapeRoomManager (em qualquer ordem)")]
        [SerializeField] private bool useEscapeRoomManagerCode = true;

        [Header("Combination Code (9 Numbers Max)")]
        [SerializeField] private int keypadCombo = 12345;

        public UnityEvent OnAccessGranted => onAccessGranted;
        public UnityEvent OnAccessDenied => onAccessDenied;

        [Header("Settings")]
        [SerializeField] private string accessGrantedText = "Granted";
        [SerializeField] private string accessDeniedText = "Denied";

        [Header("Visuals")]
        [SerializeField] private float displayResultTime = 1f;
        [Range(0, 5)]
        [SerializeField] private float screenIntensity = 2.5f;
        [Header("Colors")]
        [SerializeField] private Color screenNormalColor = new Color(0.98f, 0.50f, 0.032f, 1f); //orangy
        [SerializeField] private Color screenDeniedColor = new Color(1f, 0f, 0f, 1f); //red
        [SerializeField] private Color screenGrantedColor = new Color(0f, 0.62f, 0.07f); //greenish
        [Header("SoundFx")]
        [SerializeField] private AudioClip buttonClickedSfx;
        [SerializeField] private AudioClip accessDeniedSfx;
        [SerializeField] private AudioClip accessGrantedSfx;
        [Header("Component References")]
        [SerializeField] private Renderer panelMesh;
        [SerializeField] private TMP_Text keypadDisplayText;
        [SerializeField] private AudioSource audioSource;


        private string currentInput;
        private bool displayingResult = false;
        private bool accessWasGranted = false;

        private void Awake()
        {
            ClearInput();
            panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);
        }


        //Gets value from pressedbutton
        public void AddInput(string input)
        {
            Debug.Log($"[Keypad] input recebido: {input}");
            audioSource.PlayOneShot(buttonClickedSfx);
            if (displayingResult || accessWasGranted) return;
            switch (input)
            {
                case "enter":
                    CheckCombo();
                    break;
                default:
                    if (currentInput != null && currentInput.Length == 9) // 9 max passcode size 
                    {
                        return;
                    }
                    currentInput += input;
                    keypadDisplayText.text = currentInput;
                    break;
            }

        }

        public void CheckCombo()
        {
            bool granted = false;

            // aqui foi feita a modificação necessária para se adaptar ao projeto
            // se a opção de usar o código do EscapeRoomManager estiver ativa
            if (useEscapeRoomManagerCode)
            {
                // procura o EscapeRoomManager na cena
                EscapeRoomManager manager = FindAnyObjectByType<EscapeRoomManager>();
                // debug
                Debug.Log($"[Keypad] EscapeRoomManager encontrado: {manager != null}");
                // se o manager existir e tiver um código final definido
                if (manager != null && manager.finalCode != null)
                {
                    // debugs
                    Debug.Log($"[Keypad] código final esperado: {string.Join("", manager.finalCode)}");
                    Debug.Log($"[Keypad] input do utilizador: {currentInput}");
                    // converte o input do utilizador numa lista de inteiros
                    List<int> enteredDigits = new List<int>();
                    // percorre cada caractere do input atual e tenta convertê-lo para inteiro
                    foreach (char c in currentInput)
                    {
                        if (int.TryParse(c.ToString(), out int val))
                        {
                            enteredDigits.Add(val); // adiciona o dígito à lista de dígitos inseridos
                        }
                    }

                    // se a quantidade de dígitos for igual, verifica-se se tem os mesmos dígitos
                    if (enteredDigits.Count == manager.finalCode.Length)
                    {
                        // ordena a sequência pelo valor dos seus elementos, ou seja, em ordem crescente,
                        // e compara se a sequência inserida pelo utilizador é igual à sequência final do manager, 
                        // independentemente da ordem com que foram inseridos os dígitos
                        // o acesso é concedido se as duas sequências forem iguais
                        // baseado em: https://learn.microsoft.com/pt-br/dotnet/api/system.linq.enumerable.orderby?view=net-8.0
                        granted = enteredDigits.OrderBy(x => x).SequenceEqual(manager.finalCode.OrderBy(x => x));
                    }
                }
            }
            else
            {
                if (int.TryParse(currentInput, out var currentKombo))
                {
                    granted = currentKombo == keypadCombo;
                }
                else
                {
                    Debug.LogWarning("Couldn't process input for some reason..");
                }
            }

            if (!displayingResult)
            {
                StartCoroutine(DisplayResultRoutine(granted));
            }
        }

        //mainly for animations 
        private IEnumerator DisplayResultRoutine(bool granted)
        {
            displayingResult = true;

            if (granted) AccessGranted();
            else AccessDenied();

            yield return new WaitForSeconds(displayResultTime);
            displayingResult = false;
            if (granted) yield break;
            ClearInput();
            panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);

        }

        private void AccessDenied()
        {
            keypadDisplayText.text = accessDeniedText;
            onAccessDenied?.Invoke();
            panelMesh.material.SetVector("_EmissionColor", screenDeniedColor * screenIntensity);
            audioSource.PlayOneShot(accessDeniedSfx);
        }

        private void ClearInput()
        {
            currentInput = "";
            keypadDisplayText.text = currentInput;
        }

        private void AccessGranted()
        {
            accessWasGranted = true;
            keypadDisplayText.text = accessGrantedText;
            onAccessGranted?.Invoke();
            panelMesh.material.SetVector("_EmissionColor", screenGrantedColor * screenIntensity);
            audioSource.PlayOneShot(accessGrantedSfx);
        }

    }
}