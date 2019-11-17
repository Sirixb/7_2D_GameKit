using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gamekit2D
{
    public class ScreenFader : MonoBehaviour
    {   //Tipos de Fade
        public enum FadeType
        {
            Black, Loading, GameOver,
        }
        //Propiedad Instancia de ScreenFader ES PARA QUE SE REINICIE EL CODIGO EN START DE CORUTINAS AL CREAR NUEVAS INSTANCIAS DE ESTE MISMO.
        public static ScreenFader Instance
        {
            get
            {   //esta vacia?
                if (s_Instance != null)
                    return s_Instance;
                //si si entonces encuentre un objeto de tipo ScreeFader
                s_Instance = FindObjectOfType<ScreenFader> ();
                //sigu vacia?
                if (s_Instance != null)
                    return s_Instance;
                //sigue vacio entonces creelo
                Create ();

                return s_Instance;
            }
        }
        //propiead estatica
        public static bool IsFading
        {
            get { return Instance.m_IsFading; }
        }
       
        //declaro variable estatica de "segunda instancia"
        protected static ScreenFader s_Instance;
        //Metodo estatico s_Instance podria ser Segunda Instancia de este mismo script
        public static void Create ()
        {
            ScreenFader controllerPrefab = Resources.Load<ScreenFader> ("ScreenFader");
            s_Instance = Instantiate (controllerPrefab);//a la segunda instancia, instanciele el ScreeFader Script
        }


        public CanvasGroup faderCanvasGroup;
        public CanvasGroup loadingCanvasGroup;
        public CanvasGroup gameOverCanvasGroup;
        public float fadeDuration = 1f;

        protected bool m_IsFading;
    
        const int k_MaxSortingLayer = 32767;

        void Awake ()
        {   //Necesaro para que se cree la primera instancia al consultarla simplemente y no genere errores
            //Instancia es diferente de este?si si destruya 
            if (Instance != this)
            {
                Destroy (gameObject);
                return;
            }
            //si no no lo destruya
            DontDestroyOnLoad (gameObject);
        }
        //Fade es llamado dentro de Fade in y Fade Out es una corutina no estatica anidada: finalAlpha:0 o 1
        protected IEnumerator Fade(float finalAlpha, CanvasGroup canvasGroup)
        {
            m_IsFading = true;
            canvasGroup.blocksRaycasts = true;
            float fadeSpeed = Mathf.Abs(canvasGroup.alpha - finalAlpha) / fadeDuration;//Si el alpha es A:0-0/0.5= 0(no mueve al multi 0); B:1-0/0.5=2; C:0-1/0.5=-2(2 por ser absoluto); D:1-1/0.5=0 
            while (!Mathf.Approximately(canvasGroup.alpha, finalAlpha))//si no son similares entre
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, finalAlpha,
                    fadeSpeed * Time.deltaTime);//mueva lo a su contrario
                yield return null;//siguiente frame
            }
            canvasGroup.alpha = finalAlpha;//llevele el resultado
            m_IsFading = false;
            canvasGroup.blocksRaycasts = false;
        }
        //Establecer el alpha en 1
        public static void SetAlpha (float alpha)//llamado desde SceneContro..
        {
            Instance.faderCanvasGroup.alpha = alpha;//Pero solo de FaderCanvas
        }
        //Corutina estatica FadeIn
        public static IEnumerator FadeSceneIn ()
        {
            CanvasGroup canvasGroup;
            if (Instance.faderCanvasGroup.alpha > 0.1f)//si porque se puso en 1 en el metodo anterior
                canvasGroup = Instance.faderCanvasGroup;
            else if (Instance.gameOverCanvasGroup.alpha > 0.1f)
                canvasGroup = Instance.gameOverCanvasGroup;
            else
                canvasGroup = Instance.loadingCanvasGroup;
            
            yield return Instance.StartCoroutine(Instance.Fade(0f, canvasGroup));//inicia otra corutina anidada Fade

            canvasGroup.gameObject.SetActive (false);
        }
        //Corutina estatica FadeOut, el mimso se pasa su propio parametro fadeType, interesnate...
        public static IEnumerator FadeSceneOut (FadeType fadeType = FadeType.Black)
        {   //.Black y .Gameover es llamado desde playerChar.. cuadndo muere
            CanvasGroup canvasGroup;
            switch (fadeType)
            {
                case FadeType.Black:
                    canvasGroup = Instance.faderCanvasGroup;
                    break;
                case FadeType.GameOver:
                    canvasGroup = Instance.gameOverCanvasGroup;
                    break;
                default:
                    canvasGroup = Instance.loadingCanvasGroup;
                    break;
            }
            
            canvasGroup.gameObject.SetActive (true);
            //Usa dos veces Instance por que esta anidando corutinas la primera para ejecutar StartCoroutine dentro de FadeScen..estitco y Fade que no es estatico
            yield return Instance.StartCoroutine(Instance.Fade(1f, canvasGroup));
        }
    }
}