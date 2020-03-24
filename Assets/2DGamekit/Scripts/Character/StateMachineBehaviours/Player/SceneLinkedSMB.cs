using UnityEngine;
using UnityEngine.Animations;

namespace Gamekit2D
{
    //SMB las siglas de SceneLinked significan State Machine Behaviour 
    //TMonoBehaviour significa: Tipo MonoBehabiour
    //y este Script es generico y sirve para reemplazar el que se crea por defecto SMB(StateMachineBehaviour) añadiendo funcionalidades 
    //como poder referenciar obejtos directamente sin usar el costoso GetComponent al final de este script lo comentan
    //Clase ScenelinkdSMB de <TipoMonoBehaviour> Que es generico aun, hereda de la clase abstracta selladaSMB que a su vez hereda de StateMachineBehaviour en la parte final del script,
    //donde (where) el tipo TipoMonoBehaviour hereda de MonoBehaviour
    public class SceneLinkedSMB<TMonoBehaviour> : SealedSMB 
        where TMonoBehaviour : MonoBehaviour
    {
        protected TMonoBehaviour m_MonoBehaviour;//se declara una variable m_MonoBehaviour: de TipoMonoBehaviour, es llamada en todos los behaviours y puede heredar de cualquier script para llamar sus miembros
    
        bool m_FirstFrameHappened;
        bool m_LastFrameHappened;

        public static void Initialise (Animator animator, TMonoBehaviour monoBehaviour)
        {
            //Creamos un array de tipo Scene..en minusculas scen.. es igual a todos los Behaviour(comportamientos) de tipo Scene..entregados por ese animador en concreto (Ellen Controller), alparecer ese GetBehaviours tambien es Generico y Creado en SendSignal.cs
            SceneLinkedSMB<TMonoBehaviour>[] sceneLinkedSMBs = animator.GetBehaviours<SceneLinkedSMB<TMonoBehaviour>>();
            //Recorra el array usando la funcion InternalInicialise con el respectivo animador y clase entregada
            for (int i = 0; i < sceneLinkedSMBs.Length; i++)
            {
                //Inicialice o encienda cada comportamiento[i].llamada enviando el animador y el Script de PlayerCharacter
                sceneLinkedSMBs[i].InternalInitialise(animator, monoBehaviour);
            }
        }
        // al miembro m_MonoBehaviour asigne el monoBehaviour o clase entregada y con una funcion virtual creada mas abajo inicialice en la funcion start el animador
        protected void InternalInitialise (Animator animator, TMonoBehaviour monoBehaviour)
        {
            m_MonoBehaviour = monoBehaviour;//asigne a esta variable la clase PlayerCharacter
            OnStart (animator);//Inicialice el animador de PlayerCharacter
        }

        public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            m_FirstFrameHappened = false;

            OnSLStateEnter(animator, stateInfo, layerIndex);
            OnSLStateEnter (animator, stateInfo, layerIndex, controller);
        }

        public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            if(!animator.gameObject.activeSelf)
                return;
        
            if (animator.IsInTransition(layerIndex) && animator.GetNextAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash)
            {
                OnSLTransitionToStateUpdate(animator, stateInfo, layerIndex);
                OnSLTransitionToStateUpdate(animator, stateInfo, layerIndex, controller);
            }

            if (!animator.IsInTransition(layerIndex) && m_FirstFrameHappened)
            {
                OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);
                OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex, controller);
            }
        
            if (animator.IsInTransition(layerIndex) && !m_LastFrameHappened && m_FirstFrameHappened)
            {
                m_LastFrameHappened = true;
            
                OnSLStatePreExit(animator, stateInfo, layerIndex);
                OnSLStatePreExit(animator, stateInfo, layerIndex, controller);
            }

            if (!animator.IsInTransition(layerIndex) && !m_FirstFrameHappened)
            {
                m_FirstFrameHappened = true;

                OnSLStatePostEnter(animator, stateInfo, layerIndex);
                OnSLStatePostEnter(animator, stateInfo, layerIndex, controller);
            }

            if (animator.IsInTransition(layerIndex) && animator.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash)
            {
                OnSLTransitionFromStateUpdate(animator, stateInfo, layerIndex);
                OnSLTransitionFromStateUpdate(animator, stateInfo, layerIndex, controller);
            }
        }

        public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
        {
            m_LastFrameHappened = false;

            OnSLStateExit(animator, stateInfo, layerIndex);
            OnSLStateExit(animator, stateInfo, layerIndex, controller);
        }

        //Metodos propios del SL(Scene Linked) que reemplazan o sobre escriben el State Machine Behaviour (SMB)
        /// <summary>
        /// Called by a MonoBehaviour in the scene during its Start function.
        /// </summary>
        public virtual void OnStart(Animator animator) { }//Prende un animador

        /// <summary>
        /// Called before Updates when execution of the state first starts (on transition to the state).
        /// Llamado antes de Actualizaciones cuando comienza la ejecución del estado (en la transición al estado).
        /// </summary>
        public virtual void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        /// <summary>
        /// Called after OnSLStateEnter every frame during transition to the state.
        /// Llamado después de OnSLStateEnter cada cuadro durante la transición al estado.
        /// </summary>
        public virtual void OnSLTransitionToStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        /// <summary>
        /// Called on the first frame after the transition to the state has finished.
        /// Llamado en el primer cuadro después de que la transición al estado ha terminado.
        /// </summary>
        public virtual void OnSLStatePostEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        /// <summary>
        /// Called every frame after PostEnter when the state is not being transitioned to or from.
        /// Llamó a cada fotograma después de PostEnter cuando el estado no se está haciendo la transición hacia o desde.
        /// </summary>//Creo equivaldria al  OnStateUpdate, llamado en Locomotion, Aribone, Hurt...
        public virtual void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        /// <summary>
        /// Called on the first frame after the transition from the state has started.  Note that if the transition has a duration of less than a frame, this will not be called.
        /// Llamado en el primer fotograma después de que la transición del estado ha comenzado. Tenga en cuenta que si la transición tiene una duración inferior a un fotograma, esto no se llamará.
        /// </summary>
        public virtual void OnSLStatePreExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        /// <summary>
        /// Called after OnSLStatePreExit every frame during transition to the state.
        /// Llamado después de OnSLStatePreExit cada fotograma durante la transición al estado.
        /// </summary>
        public virtual void OnSLTransitionFromStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        /// <summary>
        /// Called after Updates when execution of the state first finshes (after transition from the state).
        /// Se llama después de las actualizaciones cuando finaliza la ejecución del estado (después de la transición desde el estado).
        /// </summary>
        public virtual void OnSLStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }


        //SE REPITE: PERO CON  AnimatorControllerPlayable controller como parametro
        /// <summary>
        /// Called before Updates when execution of the state first starts (on transition to the state).
        /// </summary>
        public virtual void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

        /// <summary>
        /// Called after OnSLStateEnter every frame during transition to the state.
        /// </summary>
        public virtual void OnSLTransitionToStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

        /// <summary>
        /// Called on the first frame after the transition to the state has finished.
        /// </summary>
        public virtual void OnSLStatePostEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

        /// <summary>
        /// Called every frame when the state is not being transitioned to or from.
        /// </summary>
        public virtual void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

        /// <summary>
        /// Called on the first frame after the transition from the state has started.  Note that if the transition has a duration of less than a frame, this will not be called.
        /// </summary>
        public virtual void OnSLStatePreExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

        /// <summary>
        /// Called after OnSLStatePreExit every frame during transition to the state.
        /// </summary>
        public virtual void OnSLTransitionFromStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

        /// <summary>
        /// Called after Updates when execution of the state first finshes (after transition from the state).
        /// </summary>
        public virtual void OnSLStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }
    }
    //Aca se habla de que reemplaza el StateMachineBehaviour, añadiendo la posibilidad de referenciar objetos mientras el estado esta encendido 
    //evitando el costo de recuperarlo a traves de un getcomponent cada vez
    //This class repalce normal StateMachineBehaviour. It add the possibility of having direct reference to the object
    //the state is running on, avoiding the cost of retrienving it through a GetComponent every time.
    //c.f. Documentation for more in depth explainations.
    //Clase abstracta que hereda del StateMachineBehaviour
    public abstract class SealedSMB : StateMachineBehaviour
    {
        public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

        public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    }
}