using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    public class ChomperPatrolSMB : SceneLinkedSMB<EnemyBehaviour>//todo m_MonoBehaviour aca hereda de EnemyBehaviour
    {
        public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Hacemos esto explícitamente aquí en lugar de en la clase enemiga, eso permite manejar los obstáculos de manera diferente según el estado
            //We do this explicitly here instead of in the enemy class, that allow to handle obstacle differently according to state
            // (por ejemplo, mira el ChomperRunToTargetSMB que detiene la persecución si hay un obstáculo)
            // (e.g. look at the ChomperRunToTargetSMB that stop the pursuit if there is an obstacle) 
            float dist = m_MonoBehaviour.speed;
            if (m_MonoBehaviour.CheckForObstacle(dist))//Comprueba si hay obstaculos y redirecciona al personaje
            {
                //this will inverse the move vector, and UpdateFacing will then flip the sprite & forward vector as moveVector will be in the other direction
                m_MonoBehaviour.SetHorizontalSpeed(-dist);
                m_MonoBehaviour.UpdateFacing();//Actualiza la cara
            }
            else
            {
                m_MonoBehaviour.SetHorizontalSpeed(dist);
            }
            //comprube si el jugador esta a la vista mientras estoy en estado de caminar
            m_MonoBehaviour.ScanForPlayer();
        }
    }
}