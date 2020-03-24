using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{   //Llamado por MeleeAttack() en PlayerCharacter  despues de activar el parametro del animator MeleeAttack a su vez desde locomotion
    public class MeleeAttackSMB : SceneLinkedSMB<PlayerCharacter>
    {   //Hash del estado de ataque aereo
        int m_HashAirborneMeleeAttackState = Animator.StringToHash ("AirborneMeleeAttack");
    
        public override void OnSLStatePostEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_MonoBehaviour.ForceNotHoldingGun();//Fuerza no pasar al estado de arma
            m_MonoBehaviour.EnableMeleeAttack();//Habilita el ataque del jugador
            m_MonoBehaviour.SetHorizontalMovement(m_MonoBehaviour.meleeAttackDashSpeed * m_MonoBehaviour.GetFacing());//establece movimiento personalizado
        }

        public override void OnSLStateNoTransitionUpdate (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {   //Si no esta en el piso active 
            if (!m_MonoBehaviour.CheckForGrounded ())
                animator.Play (m_HashAirborneMeleeAttackState, layerIndex, stateInfo.normalizedTime);//reproduzca el estado de ataque aereo
        
            m_MonoBehaviour.GroundedHorizontalMovement (false);//detiene el input de movimiento horizontal
        }

        public override void OnSLStateExit (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_MonoBehaviour.DisableMeleeAttack();//Deshabilita el ataque del jugador
        }
    }
}