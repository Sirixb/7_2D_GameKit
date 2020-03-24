using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    public class HurtSMB : SceneLinkedSMB<PlayerCharacter>
    {
        public override void OnSLStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {   //Establece el movimiento * el salto de daño
            m_MonoBehaviour.SetMoveVector(m_MonoBehaviour.GetHurtDirection() * m_MonoBehaviour.hurtJumpSpeed);//Establece el movimiento directamente sin GroundedHorizontalMovement osea sin las direccionales
            m_MonoBehaviour.StartFlickering ();//comienza el parapadeo
        }

        public override void OnSLStateNoTransitionUpdate (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {   //si esta cayendo
            if(m_MonoBehaviour.IsFalling ())
                m_MonoBehaviour.CheckForGrounded();//compruebe piso
            m_MonoBehaviour.AirborneVerticalMovement();//Gravedad
        }
    }
}