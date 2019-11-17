using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    public class AirborneSMB : SceneLinkedSMB<PlayerCharacter>
    {
        public override void OnSLStateNoTransitionUpdate (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_MonoBehaviour.UpdateFacing();//Rotación
            m_MonoBehaviour.UpdateJump();//Salto6. actualice el salto y si esta presionado
            m_MonoBehaviour.AirborneHorizontalMovement();//Salto8. verifique la horizontal deseada
            m_MonoBehaviour.AirborneVerticalMovement();//Salto10. Verifique si choco contra techo
            m_MonoBehaviour.CheckForGrounded();//Comprueba Suelo
            m_MonoBehaviour.CheckForHoldingGun();
            if(m_MonoBehaviour.CheckForMeleeAttackInput())
                m_MonoBehaviour.MeleeAttack ();
            m_MonoBehaviour.CheckAndFireGun ();
            m_MonoBehaviour.CheckForCrouching ();
        }
    }
}