using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NavKeypad
{
    /// <summary>
    /// Classe que representa uma porta virtual com animação
    /// </summary>
    public class SlidingDoor : MonoBehaviour
    {
        [SerializeField] private Animator anim; // referência ao componente Animator da porta virtual
        public bool IsOpen => isOpen; // propriedade pública para verificar se a porta está aberta
        private bool isOpen = false; // flag para indicar se a porta está aberta ou fechada

        /// <summary>
        /// Alterna o estado da porta entre aberta e fechada
        /// </summary>
        public void ToggleDoor()
        {
            isOpen = !isOpen;
            anim.SetBool("isOpen", isOpen);
        }

        /// <summary>
        /// Abre a porta virtual, definindo a flag isOpen como true 
        /// e atualizando o parâmetro "isOpen" do Animator
        /// </summary>
        public void OpenDoor()
        {
            isOpen = true;
            anim.SetBool("isOpen", isOpen);
        }
        /// <summary>
        /// Fecha a porta virtual, definindo a flag isOpen como false 
        /// e atualizando o parâmetro "isOpen" do Animator
        /// </summary>
        public void CloseDoor()
        {
            isOpen = false;
            anim.SetBool("isOpen", isOpen);
        }
    }
}