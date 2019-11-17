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
            m_MonoBehaviour.GroundedHorizontalMovement(true);//Movimiento Horizontal 1 a tierra
            m_MonoBehaviour.GroundedVerticalMovement();//MovimientoVertical1 a tierra (gravedad)
            m_MonoBehaviour.CheckForCrouching();
            m_MonoBehaviour.CheckForGrounded();//Suelo1. Comprueba piso
            m_MonoBehaviour.CheckForPushing();
            m_MonoBehaviour.CheckForHoldingGun();
            m_MonoBehaviour.CheckAndFireGun ();
            if (m_MonoBehaviour.CheckForJumpInput ())//Salto1. cumprueba el input de salto tipo bool
                m_MonoBehaviour.SetVerticalMovement(m_MonoBehaviour.jumpSpeed);//Salto3. evita el doble salto, asigna la variable jumpSpeed al vector en eje y, la variable grounded se pone en false y me manda al siguiente estado: AirboneSMB.cs por lo que esta instrucción no se repite y no genera mas altura
            else if(m_MonoBehaviour.CheckForMeleeAttackInput ())
                m_MonoBehaviour.MeleeAttack();
        }
    }
}