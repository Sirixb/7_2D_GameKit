using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class CharacterController2D : MonoBehaviour
    {
        [Tooltip("The Layers which represent gameobjects that the Character Controller can be grounded on.")]
        public LayerMask groundedLayerMask;
        [Tooltip("The distance down to check for ground.")]
        public float groundedRaycastDistance = 0.1f;

        Rigidbody2D m_Rigidbody2D;
        CapsuleCollider2D m_Capsule;
        Vector2 m_PreviousPosition;
        Vector2 m_CurrentPosition;
        Vector2 m_NextMovement;
        ContactFilter2D m_ContactFilter;
        RaycastHit2D[] m_HitBuffer = new RaycastHit2D[5];//Almacen temporal de resultados del Raycast que incluso puede atravesar y encontrar mas impactos
        RaycastHit2D[] m_FoundHits = new RaycastHit2D[3];//Almacena los HitBuffer o Golpes Bufffer
        Collider2D[] m_GroundColliders = new Collider2D[3];
        Vector2[] m_RaycastPositions = new Vector2[3];

        public bool IsGrounded { get; protected set; }
        public bool IsCeilinged { get; protected set; }
        public Vector2 Velocity { get; protected set; }
        public Rigidbody2D Rigidbody2D { get { return m_Rigidbody2D; } }
        public Collider2D[] GroundColliders { get { return m_GroundColliders; } }//arriba se creo esta variable miembro
        public ContactFilter2D ContactFilter { get { return m_ContactFilter; } }//arriba se creo esta variable miembro


        void Awake()
        {
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
            m_Capsule = GetComponent<CapsuleCollider2D>();
            //posicion del rigidbody2D
            m_CurrentPosition = m_Rigidbody2D.position;
            m_PreviousPosition = m_Rigidbody2D.position;
            //configuración de contactos asignados a la propiedad ContactoFiltro
            m_ContactFilter.layerMask = groundedLayerMask;
            m_ContactFilter.useLayerMask = true;
            m_ContactFilter.useTriggers = false;
            //Para que no se detecte asi mismo - collider del personaje
            Physics2D.queriesStartInColliders = false;
        }

        void FixedUpdate()
        {   //Posicion actual del Rigidbody
            m_PreviousPosition = m_Rigidbody2D.position;
            //Se necesta la posicion previa y la siguiente para que el rigidbody sepa donde debe estar
            m_CurrentPosition = m_PreviousPosition + m_NextMovement;// proximoMov se configura en el metodo abajo y contiene el valor real
            Velocity = (m_CurrentPosition - m_PreviousPosition) / Time.deltaTime;//es usado mas abajo para saber si es piso
            //Mueve el rigidbody2D
            m_Rigidbody2D.MovePosition(m_CurrentPosition);
            m_NextMovement = Vector2.zero;//resetea el movimiento acumulado 
            //aqui va funcion de compruebe colisines al final de la capsula
            CheckCapsuleEndCollisions();
            CheckCapsuleEndCollisions(false);//Muestra los rayos para arriba de una vez para detectar techo
        }
        //Movimineto del Rigidbody
        /// <summary>
        /// This moves a rigidbody and so should only be called from FixedUpdate or other Physics messages.
        /// </summary>
        /// <param name="movement">The amount moved in global coordinates relative to the rigidbody2D's position.</param>
        public void Move(Vector2 movement)
        {
            m_NextMovement += movement;//acumulador en vector 2
        }

        /// <summary>
        /// This moves the character without any implied velocity.
        /// </summary>
        /// <param name="position">The new position of the character in global space.</param>
        public void Teleport(Vector2 position)
        {
            Vector2 delta = position - m_CurrentPosition;
            m_PreviousPosition += delta;
            m_CurrentPosition = position;
            m_Rigidbody2D.MovePosition(position);
        }

        /// <summary>
        /// This updates the state of IsGrounded.  It is called automatically in FixedUpdate but can be called more frequently if higher accurracy is required.
        /// </summary>
        public void CheckCapsuleEndCollisions(bool bottom = true)
        {
            Vector2 raycastDirection;
            Vector2 raycastStart;
            float raycastDistance;
            //Si el componente CapsuleCollider2D no esta
            if (m_Capsule == null)
            {
                //inicio de rayo comienza con la posicion del rigid + la compensación
                raycastStart = m_Rigidbody2D.position + Vector2.up;
                raycastDistance = 1f + groundedRaycastDistance;
                
                if (bottom)
                {
                    raycastDirection = Vector2.down;

                    m_RaycastPositions[0] = raycastStart + Vector2.left * 0.4f;
                    m_RaycastPositions[1] = raycastStart;
                    m_RaycastPositions[2] = raycastStart + Vector2.right * 0.4f;
                }
                else
                {
                    raycastDirection = Vector2.up;

                    m_RaycastPositions[0] = raycastStart + Vector2.left * 0.4f;
                    m_RaycastPositions[1] = raycastStart;
                    m_RaycastPositions[2] = raycastStart + Vector2.right * 0.4f;
                }
            }
            else//si hay componente CapsulaCollider 2D
            {
                //inicio de rayo comienza con la posicion del rigid + la compensación
                raycastStart = m_Rigidbody2D.position + m_Capsule.offset;//encuentre el centro del personaje
                raycastDistance = m_Capsule.size.x * 0.5f + groundedRaycastDistance * 2f; //valor de mitad del personaje en x + rayo a Piso= .1f *2f(supungo que para anticipar mas el rayo)

                if (bottom)
                {
                    //Calcule el inicio del rayo en el centro inferior, multiplico down(-) por la mitad Y y a Y le quita el valor de la mitad de X del tamaño del collider
                    raycastDirection = Vector2.down;
                    Vector2 raycastStartBottomCentre = raycastStart + Vector2.down * (m_Capsule.size.y * 0.5f - m_Capsule.size.x * 0.5f);
                    //Pondra los rayos justo antes de la curvatura de la capsula en la parte inferior
                    m_RaycastPositions[0] = raycastStartBottomCentre + Vector2.left * m_Capsule.size.x * 0.5f;
                    m_RaycastPositions[1] = raycastStartBottomCentre;
                    m_RaycastPositions[2] = raycastStartBottomCentre + Vector2.right * m_Capsule.size.x * 0.5f;
                }
                else
                {
                    raycastDirection = Vector2.up;
                    Vector2 raycastStartTopCentre = raycastStart + Vector2.up * (m_Capsule.size.y * 0.5f - m_Capsule.size.x * 0.5f);
                    //Igual que en el if pero arriba
                    m_RaycastPositions[0] = raycastStartTopCentre + Vector2.left * m_Capsule.size.x * 0.5f;
                    m_RaycastPositions[1] = raycastStartTopCentre;
                    m_RaycastPositions[2] = raycastStartTopCentre + Vector2.right * m_Capsule.size.x * 0.5f;
                }
            }

            //Emicion de Rayo y Colisionadores en el piso
            for (int i = 0; i < m_RaycastPositions.Length; i++)
            {
                //Cuenta la cantidad de objetos golpeados
                int count = Physics2D.Raycast(m_RaycastPositions[i], raycastDirection, m_ContactFilter, m_HitBuffer, raycastDistance);

                if (bottom)//Si es piso almacene esos colliders.
                {
                    //Encontro mas de un golpe? si si:almacene ese golpetemporal encontrado, sino:almacene un raycashit2d vacio
                    m_FoundHits[i] = count > 0 ? m_HitBuffer[0] : new RaycastHit2D();//almacene el primer objeto golpeado de cada uno de los 3 rayos
                    m_GroundColliders[i] = m_FoundHits[i].collider; //Almacene los collider de los primeros golpes encontrados
                }
                else//Es techo
                {
                    IsCeilinged = false;//si va en el aire y no ha golpeado nada
                    //hasta la cantidad de golpes
                    for (int j = 0; j < m_HitBuffer.Length; j++)
                    {
                        if (m_HitBuffer[j].collider != null) //Si lo que golpeo tiene un collider
                        {   //Creo dice si es diferente a una plataforma mobil entonces es techo
                            if (!PhysicsHelper.ColliderHasPlatformEffector(m_HitBuffer[j].collider))//Cache para plataformas
                            {
                                IsCeilinged = true;//Es techo
                            }
                        }
                    }
                }
            }
            //Determina normales para saber si esta en el piso
            if (bottom)
            {
                Vector2 groundNormal = Vector2.zero;
                int hitCount = 0;

                for (int i = 0; i < m_FoundHits.Length; i++)
                {
                    if (m_FoundHits[i].collider != null)//Si hay colliders encontrados en el primer impacto de cada rayo
                    {
                        groundNormal += m_FoundHits[i].normal;//Acumule en el vector2 las normales de todos los golpes encontrados
                        hitCount++;
                    }
                }

                if (hitCount > 0)//Si hay golpes
                {
                    groundNormal.Normalize();// normalice el vector acumulador
                }
                //velocidad relativa a la plataforma mobil
                Vector2 relativeVelocity = Velocity;
                for (int i = 0; i < m_GroundColliders.Length; i++)
                {
                    if (m_GroundColliders[i] == null)//sino encuentra collider
                        continue;// salte al final del bucle evitando llamar a La clase de ayuda

                    MovingPlatform movingPlatform;

                    if (PhysicsHelper.TryGetMovingPlatform(m_GroundColliders[i], out movingPlatform))
                    {
                        relativeVelocity -= movingPlatform.Velocity / Time.deltaTime;
                        break;
                    }
                }
                //Piso. Debe determinar si esta en el piso
                //Si la normal en X e Y es 0 es porque esta en el aire no hay normales que rebotan de los colliders
                if (Mathf.Approximately(groundNormal.x, 0f) && Mathf.Approximately(groundNormal.y, 0f))
                {
                    IsGrounded = false;
                }
                else// Es piso
                {
                    IsGrounded = relativeVelocity.y <= 0f;//Es piso si hay normales rebotando y velocidad relativa en Y es menor o igual a cero, es decir si el personaje esta quieto o con gravedad ejerciendo

                    if (m_Capsule != null)//Si no tiene capsula
                    {
                        if (m_GroundColliders[1] != null)//Pero encontro un golpe en la posicion 1, supungo que cero seria el mismo
                        {
                            float capsuleBottomHeight = m_Rigidbody2D.position.y + m_Capsule.offset.y - m_Capsule.size.y * 0.5f;//en la posicion del rigidbody + la compensacion de la cap en Y - la mitad de la cap 
                            float middleHitHeight = m_FoundHits[1].point.y;//punto Y del golpe 2 ( el 1 seria [0])
                            IsGrounded &= middleHitHeight < capsuleBottomHeight + groundedRaycastDistance;//si es menor de la posicion a la capsula + distancia de rayo entonces true
                            //el operador & evalúa ambos operandos, incluso aunque el izquierdo se evalúe como false, de modo que el resultado debe ser false con independencia del valor del operando derecho.
                            //el operador && no evalúa el operando derecho si el izquierdo se evalúa como false.
                        }
                    }
                }
            }
            //inicializa o limpia el bugger de goles en el array
            for (int i = 0; i < m_HitBuffer.Length; i++)
            {
                m_HitBuffer[i] = new RaycastHit2D();
            }
        }
    }
}