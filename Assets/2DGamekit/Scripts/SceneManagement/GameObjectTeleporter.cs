using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    /// <summary>
    /// This class is used to move gameobjects from one position to another in the scene.
    /// </summary>
    public class GameObjectTeleporter : MonoBehaviour
    {
        public static GameObjectTeleporter Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindObjectOfType<GameObjectTeleporter>();

                if (instance != null)
                    return instance;

                GameObject gameObjectTeleporter = new GameObject("GameObjectTeleporter");
                instance = gameObjectTeleporter.AddComponent<GameObjectTeleporter>();

                return instance;
            }
        }

        public static bool Transitioning
        {
            get { return Instance.m_Transitioning; }
        }

        protected static GameObjectTeleporter instance;

        protected PlayerInput m_PlayerInput;
        protected bool m_Transitioning;//esta en transicion

        void Awake ()
        {
            if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            m_PlayerInput = FindObjectOfType<PlayerInput>();
        }
        //t1 varios metodos de teletransportacion
        public static void Teleport (TransitionPoint transitionPoint)
        {
            Transform destinationTransform = Instance.GetDestination (transitionPoint.transitionDestinationTag).transform;
            Instance.StartCoroutine (Instance.Transition (transitionPoint.transitioningGameObject, true, transitionPoint.resetInputValuesOnTransition, destinationTransform.position, true));
        }
        //t2 a este se llega cuando teletransporto en la misma escena
        public static void Teleport (GameObject transitioningGameObject, Transform destination)
        {
            Instance.StartCoroutine (Instance.Transition (transitioningGameObject, false, false, destination.position, false));
        }
        //t3
        public static void Teleport (GameObject transitioningGameObject, Vector3 destinationPosition)
        {
            Instance.StartCoroutine (Instance.Transition (transitioningGameObject, false, false, destinationPosition, false));
        }
        //Corutina llamada a partir de los T# anteriores
        protected IEnumerator Transition (GameObject transitioningGameObject, bool releaseControl, bool resetInputValues, Vector3 destinationPosition, bool fade)
        {
            m_Transitioning = true;//inicio la transicion del jugador
            //control suelto? no porque t2 lo mando en falso
            if (releaseControl)
            {
                if (m_PlayerInput == null)
                    m_PlayerInput = FindObjectOfType<PlayerInput> ();
                m_PlayerInput.ReleaseControl (resetInputValues);
            }
            //falso desde t2
            if(fade)
                yield return StartCoroutine (ScreenFader.FadeSceneOut ());//ponga en negro
            //el jugador se le asigna la nueva posicion
            transitioningGameObject.transform.position = destinationPosition;
        
            if(fade)//ponga en transparente
                yield return StartCoroutine (ScreenFader.FadeSceneIn ());
            //Recupere el control del personaje
            if (releaseControl)
            {
                m_PlayerInput.GainControl ();
            }

            m_Transitioning = false;//acabo la transición del jugador
        }

        protected SceneTransitionDestination GetDestination(SceneTransitionDestination.DestinationTag destinationTag)
        {
            SceneTransitionDestination[] entrances = FindObjectsOfType<SceneTransitionDestination>();
            for (int i = 0; i < entrances.Length; i++)
            {
                if (entrances[i].destinationTag == destinationTag)
                    return entrances[i];
            }
            Debug.LogWarning("No entrance was found with the " + destinationTag + " tag.");
            return null;
        }
    }
}