using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;//para manejo de escenas

namespace Gamekit2D
{
    /// <summary>
    /// This class is used to transition between scenes. This includes triggering all the things that need to happen on transition such as data persistence.
    /// </summary>
    public class SceneController : MonoBehaviour
    {   //propiedad estatica de SceneController hereda de MonoBehaviour
        public static SceneController Instance
        {
            get
            {   //si instancia es diferente de vacio devulva
                if (instance != null)
                    return instance;
                //sino asigne
                instance = FindObjectOfType<SceneController>();//

                if (instance != null)
                    return instance;

                Create ();

                return instance;
            }
        }
        //propiedad estatica publica que indica si estamos en transicion
        public static bool Transitioning
        {
            get { return Instance.m_Transitioning; }
        }
        
        //variable con los ajustes de propiedades y clase
        protected static SceneController instance;
        //metodo estatico crea una instancia del gameobject y el script SceneController
        public static SceneController Create ()
        {
            GameObject sceneControllerGameObject = new GameObject("SceneController");//añade el gameobject
            instance = sceneControllerGameObject.AddComponent<SceneController>();//añade el script

            return instance;
        }
        //variable publica de tipo SceneT.. (no usado hasta el momento pero si quiero una escena inicial la configuro aqui)
        public SceneTransitionDestination initialSceneTransitionDestination;

        protected Scene m_CurrentZoneScene;
        protected SceneTransitionDestination.DestinationTag m_ZoneRestartDestinationTag;
        protected PlayerInput m_PlayerInput;
        protected bool m_Transitioning;

        void Awake()
        {   //si la instancia creada es diferente de este script destruyalo
            if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            //sino No destruya el objeto
            DontDestroyOnLoad(gameObject);

            m_PlayerInput = FindObjectOfType<PlayerInput>();
            //Si esta vacio el proximo destino entre y encuentre el destino
            //Sirve por si quiero configurar una escena inicial
            if (initialSceneTransitionDestination != null)
            {
                SetEnteringGameObjectLocation(initialSceneTransitionDestination);
                ScreenFader.SetAlpha(1f);//establece en 1 es decir en negro
                StartCoroutine(ScreenFader.FadeSceneIn());
                initialSceneTransitionDestination.OnReachDestination.Invoke();//llama al metodo en SceneTransition...invoca un evento se puede ver en la jerarquia de los gameobjects TransitionDestinationA..B..C
            }
            else// si no configure escena inicial coja la escena activa (generalmente entra aqui, buena idea para que Alucard empiece de una en la escena que abra)
            {
                m_CurrentZoneScene = SceneManager.GetActiveScene(); //Coja la escena activa
                m_ZoneRestartDestinationTag = SceneTransitionDestination.DestinationTag.A;// Y pongala en el punto A de dicha Escena
            }
        }//fin awake

        //Resetear zona
        public static void RestartZone(bool resetHealth = true)
        {
            if(resetHealth && PlayerCharacter.PlayerInstance != null)
            {
                PlayerCharacter.PlayerInstance.damageable.SetHealth(PlayerCharacter.PlayerInstance.damageable.startingHealth);
            }

            Instance.StartCoroutine(Instance.Transition(Instance.m_CurrentZoneScene.name, true, Instance.m_ZoneRestartDestinationTag, TransitionPoint.TransitionType.DifferentZone));
        }
        //Resetear Zona con retraso
        public static void RestartZoneWithDelay(float delay, bool resetHealth = true)
        {
            Instance.StartCoroutine(CallWithDelay(delay, RestartZone, resetHealth));
        }
        //Transicion de escena, llamado en TransitionPoint y recibe dicho script
        public static void TransitionToScene(TransitionPoint transitionPoint)
        {
            //Obtenga una instancia de este mismo script e inicie una corrutina miembro que se inicio con otra instancia.transition(Consulta el Script Transi.new(nombreEscena.. toda la info necesaria.
            Instance.StartCoroutine(Instance.Transition(transitionPoint.newSceneName, transitionPoint.resetInputValuesOnTransition, transitionPoint.transitionDestinationTag, transitionPoint.transitionType));
        }
        //Obtenga transicion desde los tags
        public static SceneTransitionDestination GetDestinationFromTag(SceneTransitionDestination.DestinationTag destinationTag)
        {
            return Instance.GetDestination(destinationTag);
        }
        //Corutina de Transicion de escena lo incialisa el metodo TransitionToScene y RestartZone arriba,
        protected IEnumerator Transition(string newSceneName, bool resetInputValues, SceneTransitionDestination.DestinationTag destinationTag, TransitionPoint.TransitionType transitionType = TransitionPoint.TransitionType.DifferentZone)
        {
            m_Transitioning = true;
            PersistentDataManager.SaveAllData();//salve todos los datos
            //si el input esta vacio encuentrelo 
            if (m_PlayerInput == null)
                m_PlayerInput = FindObjectOfType<PlayerInput>();
            m_PlayerInput.ReleaseControl(resetInputValues);//Pierde el control del input liberelo reseteando los valores
            yield return StartCoroutine(ScreenFader.FadeSceneOut(ScreenFader.FadeType.Loading));//Llame la corutina de FadeSceneOut de tipo Loading
            PersistentDataManager.ClearPersisters();
            yield return SceneManager.LoadSceneAsync(newSceneName);//carge la nueva escena asincronicamente
            m_PlayerInput = FindObjectOfType<PlayerInput>(); //encuentre el input
            m_PlayerInput.ReleaseControl(resetInputValues);//libere y resetee valores
            PersistentDataManager.LoadAllData();//carge todos los datos
            SceneTransitionDestination entrance = GetDestination(destinationTag);//en una variable de tipo scirpt SceneTransitio...obtenga el destino llamando el metodo protegido mas abajo
            SetEnteringGameObjectLocation(entrance);//le paso como variable lo recogido en el metodo anterior, este nuevo metodo es llamado mas abajo
            SetupNewScene(transitionType, entrance);//configure la nueva escena, metodo mas abajo
            if(entrance != null)//en caso de que siga vacio
                entrance.OnReachDestination.Invoke();//entrance es una instancia de SceneTransitionDest... que luego llama que llama al campo OnReach.invoke que son un evento de Unity
            //Es como un llamado para asegurarse que si encunter el destino, en el inpector se especifica, ademas de llamar al script CharacterStateSetter que permite configurar al personaje una vez entre(posicion, animador, etc).
            yield return StartCoroutine(ScreenFader.FadeSceneIn());//fadeIn
            m_PlayerInput.GainControl();//obtenga control nuevamente

            m_Transitioning = false;
        }
        //Metodo con nombre del script SceneTran...Escena de destino, es llamado en corutina transition arriba
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
        //locacion de entrada del gameobject cuando entra en la nueva escena, llamado en corutina transition, variable de tiop SceneTransti...script
        protected void SetEnteringGameObjectLocation(SceneTransitionDestination entrance)
        {
            if (entrance == null)
            {
                Debug.LogWarning("Entering Transform's location has not been set.");
                return;
            }
            Transform entranceLocation = entrance.transform;
            Transform enteringTransform = entrance.transitioningGameObject.transform;
            enteringTransform.position = entranceLocation.position;
            enteringTransform.rotation = entranceLocation.rotation;
        }
        //configure la nueva escena, llamado en corutina transition
        protected void SetupNewScene(TransitionPoint.TransitionType transitionType, SceneTransitionDestination entrance)
        {
            if (entrance == null)
            {
                Debug.LogWarning("Restart information has not been set.");
                return;
            }
        
            if (transitionType == TransitionPoint.TransitionType.DifferentZone)
                SetZoneStart(entrance);//metodo mas abajo
        }
        //establezca zona de inicio, llamado en el metodo anterior
        protected void SetZoneStart(SceneTransitionDestination entrance)
        {
            m_CurrentZoneScene = entrance.gameObject.scene;
            m_ZoneRestartDestinationTag = entrance.destinationTag;
        }
        //llamada con retraso
        static IEnumerator CallWithDelay<T>(float delay, Action<T> call, T parameter)
        {
            yield return new WaitForSeconds(delay);
            call(parameter);
        }
    }
}