using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    public class ChomperRunToTargetSMB : SceneLinkedSMB<EnemyBehaviour>//todo m_MonoBehaviour aca hereda de EnemyBehaviour
    {
        public override void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSLStateEnter(animator, stateInfo, layerIndex);

            m_MonoBehaviour.OrientToTarget();//Metodo en EnemyBehaviour que orienta hacia el objetivo
        }

        public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);

            m_MonoBehaviour.CheckTargetStillVisible();//Metodo en EnemyBehaviour comprueba si el jugador es todavia visible
            m_MonoBehaviour.CheckMeleeAttack();//Metodo en EnemyBehaviour comprueba el ataque cuerpo a cuerpo

            float amount = m_MonoBehaviour.speed * 2.0f;//campo en EnemyBehaviour
            if (m_MonoBehaviour.CheckForObstacle(amount))//Metodo en EnemyBehaviour llamado en estado de caminar y de correr permite manejarlo con la velocidad segun el estado 
            {   //si hay un obstaculo olvide al objetivo
                m_MonoBehaviour.ForgetTarget();//campo en EnemyBehaviour
            }
            else
                m_MonoBehaviour.SetHorizontalSpeed(amount);//Metodo en EnemyBehaviour y aumente todo *2 de velocidad
        }

        public override void OnSLStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            base.OnSLStateExit(animator, stateInfo, layerIndex);

            m_MonoBehaviour.SetHorizontalSpeed(0);//Metodo en EnemyBehaviour y lleve a 0 la velocidad
        }
    }
}