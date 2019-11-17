using Cinemachine;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Gamekit2D
{
    [RequireComponent(typeof(Collider2D))]
    public class TransitionPoint : MonoBehaviour
    {
        public enum TransitionType
        {
            DifferentZone, DifferentNonGameplayScene, SameScene,
        }


        public enum TransitionWhen
        {
            ExternalCall, InteractPressed, OnTriggerEnter,
        }

    
        [Tooltip("This is the gameobject that will transition.  For example, the player.")]
        public GameObject transitioningGameObject;//Jugador en este caso
        [Tooltip("Whether the transition will be within this scene, to a different zone or a non-gameplay scene.")]
        public TransitionType transitionType;//enum de arriba zonadifere...
        [SceneName]//Esto es un script que hereda de PropertyAttribute
        public string newSceneName;//nombre de la nueva escena (es usado en SceneController)
        [Tooltip("The tag of the SceneTransitionDestination script in the scene being transitioned to.")]
        public SceneTransitionDestination.DestinationTag transitionDestinationTag;//A,B...
        [Tooltip("The destination in this scene that the transitioning gameobject will be teleported.")]
        public TransitionPoint destinationTransform;//Solo aparece cuando se cambia a Same Scene en Transition Type y solo deja agregar al transform del que tenga este mismo script (buen metodode validacion =) )
        [Tooltip("What should trigger the transition to start.")]
        public TransitionWhen transitionWhen;//On Trigger Enter, External call y InteractPressed
        [Tooltip("The player will lose control when the transition happens but should the axis and button values reset to the default when control is lost.")]
        public bool resetInputValuesOnTransition = true;//Pierde el control el jugador mientras se hace la transicion?
        [Tooltip("Is this transition only possible with specific items in the inventory?")]
        public bool requiresInventoryCheck;//requiere revisar el inventario (llaves o algo?)
        [Tooltip("The inventory to be checked.")]
        public InventoryController inventoryController;//si se pone el anterior en true se abre un listado para meter objects
        [Tooltip("The required items.")]
        public InventoryController.InventoryChecker inventoryCheck;//lista de inventario
    
        bool m_TransitioningGameObjectPresent;

        void Start ()
        {
            if (transitionWhen == TransitionWhen.ExternalCall)
                m_TransitioningGameObjectPresent = true;
        }
        //Si entra en la zona
        void OnTriggerEnter2D (Collider2D other)
        {   //es el jugador
            if (other.gameObject == transitioningGameObject)
            {
                m_TransitioningGameObjectPresent = true;//jugador presente

                if (ScreenFader.IsFading || SceneController.Transitioning)//si esta en desvaneciendo o en transicion retorne
                    return;

                if (transitionWhen == TransitionWhen.OnTriggerEnter)//si es OnTriggerEnter
                    TransitionInternal ();//llama a la funcion mas abajo
            }
        }

        void OnTriggerExit2D (Collider2D other)
        {   //jugador?
            if (other.gameObject == transitioningGameObject)
            {   //no hay jugador
                m_TransitioningGameObjectPresent = false;
            }
        }
        //Update para transicion con boton
        void Update ()
        {   //si esta haciendo fading devulva la ejecución
            if (ScreenFader.IsFading || SceneController.Transitioning)
                return;
            //Si no hay objeto (jugador) a transicionar devuelva la ejecución
            if(!m_TransitioningGameObjectPresent)
                return;
            //Interact pressed(interaccion con boton) es una seleccion de un submenu en el inspector
            if (transitionWhen == TransitionWhen.InteractPressed)
            {
                if (PlayerInput.Instance.Interact.Down)//  Interaccción
                {
                    TransitionInternal ();
                }
            }
        }
        //es llamado por el ontrigger u interact pressed
        protected void TransitionInternal ()
        {   // requiere llave? si no ejecute las dos opciones abajo (mismo lugar o nueva escena)
            if (requiresInventoryCheck)
            {   //Lista de chequeo?
                if(!inventoryCheck.CheckInventory (inventoryController))
                    return;
            }
            //Si escogi en la misma escena hagame un teleporter
            if (transitionType == TransitionType.SameScene)// misma escena
            {
                GameObjectTeleporter.Teleport (transitioningGameObject, destinationTransform.transform);//metodo estatico, creo me lleva a un T2 en GameObjectTeleporter...
            }
            else //si voy a otra escena llevele esta mismo script escena 
            {
                SceneController.TransitionToScene (this);//metodo estatico de SceneController para cambiar de escena, llevele este script
            }
        }
        
        public void Transition ()
        {
            if(!m_TransitioningGameObjectPresent)
                return;

            if(transitionWhen == TransitionWhen.ExternalCall)// llamada externa
                TransitionInternal ();
        }
    }
}