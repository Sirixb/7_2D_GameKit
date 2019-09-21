using UnityEngine;

namespace Gamekit2D
{
    public class LocomotionSMB : SceneLinkedSMB<PlayerCharacter>
    {
        public override void OnSLStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_MonoBehaviour.TeleportToColliderBottom();
        }

        public override void OnSLStateNoTransitionUpdate (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_MonoBehaviour.UpdateFacing();//Rotación
            m_MonoBehaviour.GroundedHorizontalMovement(true);//Movimiento Horizontal a tierra
            m_MonoBehaviour.GroundedVerticalMovement();//Movimiento Vertical a tierra (gravedad)
            m_MonoBehaviour.CheckForCrouching();
            m_MonoBehaviour.CheckForGrounded();//Comprueba piso
            m_MonoBehaviour.CheckForPushing();
            m_MonoBehaviour.CheckForHoldingGun();
            m_MonoBehaviour.CheckAndFireGun ();
            if (m_MonoBehaviour.CheckForJumpInput ())//cumprueba el input de salto tipo bool
                m_MonoBehaviour.SetVerticalMovement(m_MonoBehaviour.jumpSpeed);//asigna la variable jumpSpeed al vector en eje y
            else if(m_MonoBehaviour.CheckForMeleeAttackInput ())
                m_MonoBehaviour.MeleeAttack();
        }
    }
}