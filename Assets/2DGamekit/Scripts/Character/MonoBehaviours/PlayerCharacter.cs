﻿using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gamekit2D
{
    [RequireComponent(typeof(CharacterController2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCharacter : MonoBehaviour
    {
        static protected PlayerCharacter s_PlayerInstance;
        static public PlayerCharacter PlayerInstance { get { return s_PlayerInstance; } }

        public InventoryController inventoryController
        {
            get { return m_InventoryController; }
        }

        public SpriteRenderer spriteRenderer;
        public Damageable damageable;
        public Damager meleeDamager;
        public Transform facingLeftBulletSpawnPoint;
        public Transform facingRightBulletSpawnPoint;
        public BulletPool bulletPool;
        public Transform cameraFollowTarget;

        public float maxSpeed = 10f;
        public float groundAcceleration = 100f;
        public float groundDeceleration = 100f;
        [Range(0f, 1f)] public float pushingSpeedProportion;

        [Range(0f, 1f)] public float airborneAccelProportion;
        [Range(0f, 1f)] public float airborneDecelProportion;
        public float gravity = 50f;
        public float jumpSpeed = 20f;
        public float jumpAbortSpeedReduction = 100f;

        [Range(k_MinHurtJumpAngle, k_MaxHurtJumpAngle)] public float hurtJumpAngle = 45f;
        public float hurtJumpSpeed = 5f;
        public float flickeringDuration = 0.1f;

        public float meleeAttackDashSpeed = 5f;
        public bool dashWhileAirborne = false;

        public RandomAudioPlayer footstepAudioPlayer;
        public RandomAudioPlayer landingAudioPlayer;
        public RandomAudioPlayer hurtAudioPlayer;
        public RandomAudioPlayer meleeAttackAudioPlayer;
        public RandomAudioPlayer rangedAttackAudioPlayer;

        public float shotsPerSecond = 1f;
        public float bulletSpeed = 5f;
        public float holdingGunTimeoutDuration = 10f;
        public bool rightBulletSpawnPointAnimated = true;

        public float cameraHorizontalFacingOffset;
        public float cameraHorizontalSpeedOffset;
        public float cameraVerticalInputOffset;
        public float maxHorizontalDeltaDampTime;
        public float maxVerticalDeltaDampTime;
        public float verticalCameraOffsetDelay;

        public bool spriteOriginallyFacesLeft;

        protected CharacterController2D m_CharacterController2D;
        protected Animator m_Animator;
        protected CapsuleCollider2D m_Capsule;
        protected Transform m_Transform;
        protected Vector2 m_MoveVector;
        protected List<Pushable> m_CurrentPushables = new List<Pushable>(4);
        protected Pushable m_CurrentPushable;
        protected float m_TanHurtJumpAngle;
        protected WaitForSeconds m_FlickeringWait;
        protected Coroutine m_FlickerCoroutine;
        protected Transform m_CurrentBulletSpawnPoint;
        protected float m_ShotSpawnGap;
        protected WaitForSeconds m_ShotSpawnWait;
        protected Coroutine m_ShootingCoroutine;
        protected float m_NextShotTime;
        protected bool m_IsFiring;
        protected float m_ShotTimer;
        protected float m_HoldingGunTimeRemaining;
        protected TileBase m_CurrentSurface;
        protected float m_CamFollowHorizontalSpeed;
        protected float m_CamFollowVerticalSpeed;
        protected float m_VerticalCameraOffsetTimer;
        protected InventoryController m_InventoryController;

        protected Checkpoint m_LastCheckpoint = null;
        protected Vector2 m_StartingPosition = Vector2.zero;
        protected bool m_StartingFacingLeft = false;

        protected bool m_InPause = false;

        protected readonly int m_HashHorizontalSpeedPara = Animator.StringToHash("HorizontalSpeed");
        protected readonly int m_HashVerticalSpeedPara = Animator.StringToHash("VerticalSpeed");
        protected readonly int m_HashGroundedPara = Animator.StringToHash("Grounded");
        protected readonly int m_HashCrouchingPara = Animator.StringToHash("Crouching");
        protected readonly int m_HashPushingPara = Animator.StringToHash("Pushing");
        protected readonly int m_HashTimeoutPara = Animator.StringToHash("Timeout");
        protected readonly int m_HashRespawnPara = Animator.StringToHash("Respawn");
        protected readonly int m_HashDeadPara = Animator.StringToHash("Dead");
        protected readonly int m_HashHurtPara = Animator.StringToHash("Hurt");
        protected readonly int m_HashForcedRespawnPara = Animator.StringToHash("ForcedRespawn");
        protected readonly int m_HashMeleeAttackPara = Animator.StringToHash("MeleeAttack");
        protected readonly int m_HashHoldingGunPara = Animator.StringToHash("HoldingGun");

        protected const float k_MinHurtJumpAngle = 0.001f;
        protected const float k_MaxHurtJumpAngle = 89.999f;
        protected const float k_GroundedStickingVelocityMultiplier = 3f;    // This is to help the character stick to vertically moving platforms.Esto es para ayudar al personaje a adherirse a plataformas que se mueven verticalmente.

        //used in non alloc version of physic function
        protected ContactPoint2D[] m_ContactsBuffer = new ContactPoint2D[16];

        // MonoBehaviour Messages - called by Unity internally.
        void Awake()
        {
            s_PlayerInstance = this;//instancia del jugador se declara arriba debajo de la clase y se le manda este script

            m_CharacterController2D = GetComponent<CharacterController2D>();
            m_Animator = GetComponent<Animator>();
            m_Capsule = GetComponent<CapsuleCollider2D>();
            m_Transform = transform;
            m_InventoryController = GetComponent<InventoryController>();

            m_CurrentBulletSpawnPoint = spriteOriginallyFacesLeft ? facingLeftBulletSpawnPoint : facingRightBulletSpawnPoint;
        }

        void Start()
        {   //variables vinculadas con cuando es herido
            hurtJumpAngle = Mathf.Clamp(hurtJumpAngle, k_MinHurtJumpAngle, k_MaxHurtJumpAngle);//AnguDañoSalto
            m_TanHurtJumpAngle = Mathf.Tan(Mathf.Deg2Rad * hurtJumpAngle);//AnguDañoSalto
            m_FlickeringWait = new WaitForSeconds(flickeringDuration);//la variable es de tipo Waitforseconds para poder ser llamada en la corutina y se le asigna el float de tiempo
            //hasta que te encontre madafoca, esta es la causante de que de entrada se desactive el ataque de m_CanDamage en Damager para el jugador
            //meleeDamager.DisableDamage();//Deshabilite el daño cuerpo a cuerpo

            m_ShotSpawnGap = 1f / shotsPerSecond;
            m_NextShotTime = Time.time;
            m_ShotSpawnWait = new WaitForSeconds(m_ShotSpawnGap);
            //Camara
            if (!Mathf.Approximately(maxHorizontalDeltaDampTime, 0f))
            {
                float maxHorizontalDelta = maxSpeed * cameraHorizontalSpeedOffset + cameraHorizontalFacingOffset;
                m_CamFollowHorizontalSpeed = maxHorizontalDelta / maxHorizontalDeltaDampTime;
            }

            if (!Mathf.Approximately(maxVerticalDeltaDampTime, 0f))
            {
                float maxVerticalDelta = cameraVerticalInputOffset;
                m_CamFollowVerticalSpeed = maxVerticalDelta / maxVerticalDeltaDampTime;
            }
            //Aca se inicializa el animador en la clase Scene.. de tipo <Player..> y llamamos la funcion Inicialise
            //pasandole (el animador del personaje, y el TipoMonoBehaviour que en este caso seria esta misma clase PlayerCharacter "This" que hereda de MonoBehaviour.
            //Esta instrucción solo es llamada una vez y por este script.
            SceneLinkedSMB<PlayerCharacter>.Initialise(m_Animator, this);

            m_StartingPosition = transform.position;
            m_StartingFacingLeft = GetFacing() < 0.0f;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Pushable pushable = other.GetComponent<Pushable>();
            if (pushable != null)
            {
                m_CurrentPushables.Add(pushable);
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            Pushable pushable = other.GetComponent<Pushable>();
            if (pushable != null)
            {
                if (m_CurrentPushables.Contains(pushable))
                    m_CurrentPushables.Remove(pushable);
            }
        }

        void Update()
        {
            if (PlayerInput.Instance.Pause.Down)
            {
                if (!m_InPause)
                {
                    if (ScreenFader.IsFading)
                        return;

                    PlayerInput.Instance.ReleaseControl(false);
                    PlayerInput.Instance.Pause.GainControl();
                    m_InPause = true;
                    Time.timeScale = 0;
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("UIMenus", UnityEngine.SceneManagement.LoadSceneMode.Additive);
                }
                else
                {
                    Unpause();
                }
            }
        }

        void FixedUpdate()
        {
            //Funcion Move de CharacterController2D y le pasa el vector de movimiento
            m_CharacterController2D.Move(m_MoveVector * Time.deltaTime);//Controlador 2D Personaje Fisicas
            m_Animator.SetFloat(m_HashHorizontalSpeedPara, m_MoveVector.x);//Parametro X animador
            m_Animator.SetFloat(m_HashVerticalSpeedPara, m_MoveVector.y);//Parametro Y animador
            UpdateBulletSpawnPointPositions();
            UpdateCameraFollowTargetPosition();
        }

        public void Unpause()
        {
            //if the timescale is already > 0, we 
            if (Time.timeScale > 0)
                return;

            StartCoroutine(UnpauseCoroutine());
        }

        protected IEnumerator UnpauseCoroutine()
        {
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("UIMenus");
            PlayerInput.Instance.GainControl();
            //we have to wait for a fixed update so the pause button state change, otherwise we can get in case were the update
            //of this script happen BEFORE the input is updated, leading to setting the game in pause once again
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            m_InPause = false;
        }

        // Protected functions.
        protected void UpdateBulletSpawnPointPositions()
        {
            if (rightBulletSpawnPointAnimated)
            {
                Vector2 leftPosition = facingRightBulletSpawnPoint.localPosition;
                leftPosition.x *= -1f;
                facingLeftBulletSpawnPoint.localPosition = leftPosition;
            }
            else
            {
                Vector2 rightPosition = facingLeftBulletSpawnPoint.localPosition;
                rightPosition.x *= -1f;
                facingRightBulletSpawnPoint.localPosition = rightPosition;
            }
        }
        //Seguimiento de camara
        protected void UpdateCameraFollowTargetPosition()
        {
            float newLocalPosX;
            float newLocalPosY = 0f;

            float desiredLocalPosX = (spriteOriginallyFacesLeft ^ spriteRenderer.flipX ? -1f : 1f) * cameraHorizontalFacingOffset;
            desiredLocalPosX += m_MoveVector.x * cameraHorizontalSpeedOffset;
            if (Mathf.Approximately(m_CamFollowHorizontalSpeed, 0f))
                newLocalPosX = desiredLocalPosX;
            else
                newLocalPosX = Mathf.Lerp(cameraFollowTarget.localPosition.x, desiredLocalPosX, m_CamFollowHorizontalSpeed * Time.deltaTime);

            bool moveVertically = false;
            if (!Mathf.Approximately(PlayerInput.Instance.Vertical.Value, 0f))
            {
                m_VerticalCameraOffsetTimer += Time.deltaTime;

                if (m_VerticalCameraOffsetTimer >= verticalCameraOffsetDelay)
                    moveVertically = true;
            }
            else
            {
                moveVertically = true;
                m_VerticalCameraOffsetTimer = 0f;
            }

            if (moveVertically)
            {
                float desiredLocalPosY = PlayerInput.Instance.Vertical.Value * cameraVerticalInputOffset;
                if (Mathf.Approximately(m_CamFollowVerticalSpeed, 0f))
                    newLocalPosY = desiredLocalPosY;
                else
                    newLocalPosY = Mathf.MoveTowards(cameraFollowTarget.localPosition.y, desiredLocalPosY, m_CamFollowVerticalSpeed * Time.deltaTime);
            }

            cameraFollowTarget.localPosition = new Vector2(newLocalPosX, newLocalPosY);
        }
        //Corutina de parpadeo al ser herido
        protected IEnumerator Flicker()
        {
            float timer = 0f;
            //mientras el temporizador sea menor al tiempo de invulnerabilidad
            while (timer < damageable.invulnerabilityDuration)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;//active y desactive el sprite
                yield return m_FlickeringWait;//por cada 0.05 asignados en la variable tipo wait for secongs a su vez asignada por un float de tiempo
                timer += flickeringDuration;//Acumule cada vez 0.05
            }

            spriteRenderer.enabled = true;//deje habilitado  el render
        }

        protected IEnumerator Shoot()
        {
            while (PlayerInput.Instance.RangedAttack.Held)
            {
                if (Time.time >= m_NextShotTime)
                {
                    SpawnBullet();
                    m_NextShotTime = Time.time + m_ShotSpawnGap;
                }
                yield return null;
            }
        }

        protected void SpawnBullet()
        {
            //we check if there is a wall between the player and the bullet spawn position, if there is, we don't spawn a bullet
            //otherwise, the player can "shoot throught wall" because the arm extend to the other side of the wall
            Vector2 testPosition = transform.position;
            testPosition.y = m_CurrentBulletSpawnPoint.position.y;
            Vector2 direction = (Vector2)m_CurrentBulletSpawnPoint.position - testPosition;
            float distance = direction.magnitude;
            direction.Normalize();

            RaycastHit2D[] results = new RaycastHit2D[12];
            if (Physics2D.Raycast(testPosition, direction, m_CharacterController2D.ContactFilter, results, distance) > 0)
                return;

            BulletObject bullet = bulletPool.Pop(m_CurrentBulletSpawnPoint.position);
            bool facingLeft = m_CurrentBulletSpawnPoint == facingLeftBulletSpawnPoint;
            bullet.rigidbody2D.velocity = new Vector2(facingLeft ? -bulletSpeed : bulletSpeed, 0f);
            bullet.spriteRenderer.flipX = facingLeft ^ bullet.bullet.spriteOriginallyFacesLeft;

            rangedAttackAudioPlayer.PlayRandomSound();
        }
        // Funciones públicas: llamadas principalmente por StateMachineBehaviours en el Animator Controller del personaje, pero también por Events.
        // Public functions - called mostly by StateMachineBehaviours in the character's Animator Controller but also by Events.
        public void SetMoveVector(Vector2 newMoveVector)
        {
            m_MoveVector = newMoveVector;
        }

        public void SetHorizontalMovement(float newHorizontalMovement)
        {
            m_MoveVector.x = newHorizontalMovement;
        }
        //Salto4. Establecer el valor del salto y hace pasar el estado a AirbonerneSMB evitando el doble salto o salto infinito
        public void SetVerticalMovement(float newVerticalMovement)//recibe la variable JumpSpeed desde LocomotionSMB.cs
        {
            m_MoveVector.y = newVerticalMovement;
        }

        public void IncrementMovement(Vector2 additionalMovement)
        {
            m_MoveVector += additionalMovement;
        }

        public void IncrementHorizontalMovement(float additionalHorizontalMovement)
        {
            m_MoveVector.x += additionalHorizontalMovement;
        }

        public void IncrementVerticalMovement(float additionalVerticalMovement)
        {
            m_MoveVector.y += additionalVerticalMovement;
        }
        //MVertical2 en piso (Gravedad1 en piso)
        public void GroundedVerticalMovement()
        {   //Baje por efecto de gravedad
            m_MoveVector.y -= gravity * Time.deltaTime;//0 = 0 -38 * 0.02= Cada frame restele 0.76f 

            //Plataformas moviles verticales
            //Si la cantidad acumulada en Y es menor a la de la gravedad: -1 < -38 * 0.02 * 3= -2.28
            if (m_MoveVector.y < -gravity * Time.deltaTime * k_GroundedStickingVelocityMultiplier)
            {   //aplique esa misma cantidad: -2.28 (K_groun sirve para que no se atasque en las plataformas verticales)
                m_MoveVector.y = -gravity * Time.deltaTime * k_GroundedStickingVelocityMultiplier;
            }
           // print("sin gravedad piso");
        }

        public Vector2 GetMoveVector()
        {
            return m_MoveVector;
        }
        //Retorne el estado en un bool: si esta cayendo y si el parametro de suelo es falso
        public bool IsFalling()
        {   //Estoy cayendo en el eje y Y el parametro de piso no esta activo
            return m_MoveVector.y < 0f && !m_Animator.GetBool(m_HashGroundedPara);
        }
        //Rotación
        public void UpdateFacing()
        {   //Determina la cara segun el input
            bool faceLeft = PlayerInput.Instance.Horizontal.Value < 0f;
            bool faceRight = PlayerInput.Instance.Horizontal.Value > 0f;

            if (faceLeft)
            {
                spriteRenderer.flipX = !spriteOriginallyFacesLeft;//Cara personaje: usa un bool !spri..
                m_CurrentBulletSpawnPoint = facingLeftBulletSpawnPoint;//Spawn de Arma
            }
            else if (faceRight)
            {
                spriteRenderer.flipX = spriteOriginallyFacesLeft;
                m_CurrentBulletSpawnPoint = facingRightBulletSpawnPoint;
            }
        }
        //Continuacion Rotación este podria obligar la rotacion por ejemplo por un enemigo que agrede
        public void UpdateFacing(bool faceLeft)
        {   //si cara izquierda es true
            if (faceLeft)
            {   //Flip el render hacia la izquierda
                spriteRenderer.flipX = !spriteOriginallyFacesLeft;
                m_CurrentBulletSpawnPoint = facingLeftBulletSpawnPoint;//voltee tambien el spawn del arma
            }
            else
            {
                spriteRenderer.flipX = spriteOriginallyFacesLeft;
                m_CurrentBulletSpawnPoint = facingRightBulletSpawnPoint;
            }
        }
        //Obtener Cara (Continuacion Rotación)
        public float GetFacing()
        {
            return spriteRenderer.flipX != spriteOriginallyFacesLeft ? -1f : 1f;
        }
        //MHorizontal2: useInput llega en true desde LocomotionSMB
        public void GroundedHorizontalMovement(bool useInput, float speedScale = 1f)//el bool seguro sirve para validadr el poder controlar el jugador
        {
            //VelocidadDeseada = Hay input del usuario?(true por defecto en varios monobehaviours como ej Locomotion) si si asigne input 1 * 7 *1f(variable local)= 7 o -7  sino  0f
            float desiredSpeed = useInput ? PlayerInput.Instance.Horizontal.Value * maxSpeed * speedScale : 0f;
            //AceleraciOn = puede usar input y esta recibiendo inpunt? aceleraciónSuelo, sino desaceleración.
            float acceleration = useInput && PlayerInput.Instance.Horizontal.ReceivingInput ? groundAcceleration : groundDeceleration;
            // Mueva horizontalmente(0,7,100 * 0.02f=2f)= 7 ó -7
            m_MoveVector.x = Mathf.MoveTowards(m_MoveVector.x, desiredSpeed, acceleration * Time.deltaTime);
           
        }
        //Comprobacion para agacharse
        public void CheckForCrouching()
        {
            m_Animator.SetBool(m_HashCrouchingPara, PlayerInput.Instance.Vertical.Value < 0f);
        }
        //Suelo1 y Salto5. Se envia al animator el resultado de si esta en piso y el audio
        public bool CheckForGrounded()
        {   //Estaba en el suelo
            bool wasGrounded = m_Animator.GetBool(m_HashGroundedPara);
            bool grounded = m_CharacterController2D.IsGrounded;
            //Si es piso
            if (grounded)
            {   //Encuentre la superficie actual
                FindCurrentSurface();
                //Si no estaba en el suelo y el vector en y es < a -1.0f
                if (!wasGrounded && m_MoveVector.y < -1.0f)
                {//only play the landing sound if falling "fast" enough (avoid small bump playing the landing sound)
                    landingAudioPlayer.PlayRandomSound(m_CurrentSurface);
                }
            }
            else
                m_CurrentSurface = null;
            //Asigne si esta en el suelo o no desde pasa tambien al salto o Airborne
            m_Animator.SetBool(m_HashGroundedPara, grounded);
            //devuelva el estado de suelo
            return grounded;
        }
        //Encontrar superficie
        public void FindCurrentSurface()
        {
            Collider2D groundCollider = m_CharacterController2D.GroundColliders[0];

            if (groundCollider == null)
                groundCollider = m_CharacterController2D.GroundColliders[1];

            if (groundCollider == null)
                return;

            TileBase b = PhysicsHelper.FindTileForOverride(groundCollider, transform.position, Vector2.down);
            if (b != null)
            {
                m_CurrentSurface = b;
            }
        }

        public void CheckForPushing()
        {
            bool pushableOnCorrectSide = false;
            Pushable previousPushable = m_CurrentPushable;

            m_CurrentPushable = null;

            if (m_CurrentPushables.Count > 0)
            {
                bool movingRight = PlayerInput.Instance.Horizontal.Value > float.Epsilon;
                bool movingLeft = PlayerInput.Instance.Horizontal.Value < -float.Epsilon;

                for (int i = 0; i < m_CurrentPushables.Count; i++)
                {
                    float pushablePosX = m_CurrentPushables[i].pushablePosition.position.x;
                    float playerPosX = m_Transform.position.x;
                    if (pushablePosX < playerPosX && movingLeft || pushablePosX > playerPosX && movingRight)
                    {
                        pushableOnCorrectSide = true;
                        m_CurrentPushable = m_CurrentPushables[i];
                        break;
                    }
                }

                if (pushableOnCorrectSide)
                {
                    Vector2 moveToPosition = movingRight ? m_CurrentPushable.playerPushingRightPosition.position : m_CurrentPushable.playerPushingLeftPosition.position;
                    moveToPosition.y = m_CharacterController2D.Rigidbody2D.position.y;
                    m_CharacterController2D.Teleport(moveToPosition);
                }
            }

            if(previousPushable != null && m_CurrentPushable != previousPushable)
            {//we changed pushable (or don't have one anymore), stop the old one sound
                previousPushable.EndPushing();
            }

            m_Animator.SetBool(m_HashPushingPara, pushableOnCorrectSide);
        }

        public void MovePushable()
        {
            //we don't push ungrounded pushable, avoid pushing floating pushable or falling pushable.
            if (m_CurrentPushable && m_CurrentPushable.Grounded)
                m_CurrentPushable.Move(m_MoveVector * Time.deltaTime);
        }

        public void StartPushing()
        {
            if (m_CurrentPushable)
                m_CurrentPushable.StartPushing();
        }

        public void StopPushing()
        {
            if(m_CurrentPushable)
                m_CurrentPushable.EndPushing();
        }
        //Salto7. Actualiza el salto para controlar la intensidad del salto y reduzca el vuelo aumentando la gravedad
        public void UpdateJump()
        {
            //Si no esta sostenido el boton de salto y estoy en el aire, mayor a la maxima altura (intensidad de salto)
            // o suelto el boton de salto mientras esta subiendo
            if (!PlayerInput.Instance.Jump.Held && m_MoveVector.y > 0.0f)//
            {
                //print("gravedad solte boton");
                //Gravedad2 (aplicada en aire): Aumnete la velocidad de caida o aplique mas gravedad: y= y - 100*0.02=2 ( la gravedad normale era 0.76f)
                m_MoveVector.y -= jumpAbortSpeedReduction * Time.deltaTime;
            }
            
        }
        //Salto9. Movimiento Horizontal aereo deseado
        public void AirborneHorizontalMovement()
        {   // velocidad deseada= 7
            float desiredSpeed = PlayerInput.Instance.Horizontal.Value * maxSpeed;

            float acceleration;
            //Si hay inpunt horizontal en el aire
            if (PlayerInput.Instance.Horizontal.ReceivingInput)
                //100 * 1= 100
                acceleration = groundAcceleration * airborneAccelProportion;
            else//100 * 0.5= 50
                acceleration = groundDeceleration * airborneDecelProportion;
            //0, 7, 100 * 0.02= 2 //7,0,50 *0.02= 1
            m_MoveVector.x = Mathf.MoveTowards(m_MoveVector.x, desiredSpeed, acceleration * Time.deltaTime);
        }
        //Salto11. Movimiento aereo vetical (maxima altura o techo y gravedad en aire)
        public void AirborneVerticalMovement()
        {   //Si el vector en Y es aproximadamente 0 ó golpea techo y el vectorY es mayor a 0 osea esta todavia subiendo
            if (Mathf.Approximately(m_MoveVector.y, 0f) || m_CharacterController2D.IsCeilinged && m_MoveVector.y > 0f)
            {
                //Gravedad4 (aire) Golpeo algo o alcanzo maxima altura
                m_MoveVector.y = 0f;// establezcalo en cero
                
            }
            //Gravedad3 (aire) Aplica gravedad mientras se esta en estado de salto,Evita Salto infinito
            m_MoveVector.y -= gravity* Time.deltaTime;//aplique gravedad normal: y= y-38*0.02=-0.76
            
        }
        //Salto2. Comprobar el input de salto, devuelve bool true si es presionado, es llamado en LocomotionSMB...
        public bool CheckForJumpInput()
        {
            return PlayerInput.Instance.Jump.Down;
        }

        public bool CheckForFallInput()
        {
            return PlayerInput.Instance.Vertical.Value < -float.Epsilon && PlayerInput.Instance.Jump.Down;
        }
        //Pasar a traves de la plataforma
        public bool MakePlatformFallthrough()
        {
            int colliderCount = 0;
            int fallthroughColliderCount = 0;
        
            for (int i = 0; i < m_CharacterController2D.GroundColliders.Length; i++)
            {
                Collider2D col = m_CharacterController2D.GroundColliders[i];
                if(col == null)
                    continue;

                colliderCount++;

                if (PhysicsHelper.ColliderHasPlatformEffector (col))
                    fallthroughColliderCount++;
            }

            if (fallthroughColliderCount == colliderCount)
            {
                for (int i = 0; i < m_CharacterController2D.GroundColliders.Length; i++)
                {
                    Collider2D col = m_CharacterController2D.GroundColliders[i];
                    if (col == null)
                        continue;

                    PlatformEffector2D effector;
                    PhysicsHelper.TryGetPlatformEffector (col, out effector);
                    FallthroughReseter reseter = effector.gameObject.AddComponent<FallthroughReseter>();
                    reseter.StartFall(effector);
                    //set invincible for half a second when falling through a platform, as it will make the player "standup"
                    StartCoroutine(FallThroughtInvincibility());
                }
            }

            return fallthroughColliderCount == colliderCount;
        }

        IEnumerator FallThroughtInvincibility()
        {
            damageable.EnableInvulnerability(true);
            yield return new WaitForSeconds(0.5f);
            damageable.DisableInvulnerability();
        }

        public bool CheckForHoldingGun()
        {
            bool holdingGun = false;

            if (PlayerInput.Instance.RangedAttack.Held)
            {
                holdingGun = true;
                m_Animator.SetBool(m_HashHoldingGunPara, true);
                m_HoldingGunTimeRemaining = holdingGunTimeoutDuration;
            }
            else
            {
                m_HoldingGunTimeRemaining -= Time.deltaTime;

                if (m_HoldingGunTimeRemaining <= 0f)
                {
                    m_Animator.SetBool(m_HashHoldingGunPara, false);
                }
            }

            return holdingGun;
        }

        public void CheckAndFireGun()
        {
            if (PlayerInput.Instance.RangedAttack.Held && m_Animator.GetBool(m_HashHoldingGunPara))
            {
                if (m_ShootingCoroutine == null)
                    m_ShootingCoroutine = StartCoroutine(Shoot());
            }

            if ((PlayerInput.Instance.RangedAttack.Up || !m_Animator.GetBool(m_HashHoldingGunPara)) && m_ShootingCoroutine != null)
            {
                StopCoroutine(m_ShootingCoroutine);
                m_ShootingCoroutine = null;
            }
        }
        //forzar que no se pase al substate de arma, llamado por MeleeAttack
        public void ForceNotHoldingGun()
        {
            m_Animator.SetBool(m_HashHoldingGunPara, false);
        }

        public void EnableInvulnerability()
        {
            damageable.EnableInvulnerability();
        }

        public void DisableInvulnerability()
        {
            damageable.DisableInvulnerability();
        }
        //obtenga la direccion del daño
        public Vector2 GetHurtDirection()
        {   //Obtiene de damageable la direccion del daño
            Vector2 damageDirection = damageable.GetDamageDirection();
            //si el ataque viene desde encima retorne la direccion de X y 0 en Y
            if (damageDirection.y < 0f)
                return new Vector2(Mathf.Sign(damageDirection.x), 0f);
            //Para Y = valor absoluto de la direccion es decir positivo * tangente del angulo configurado en el start
            float y = Mathf.Abs(damageDirection.x) * m_TanHurtJumpAngle;/*valor en posivito de x (osea hacia arriba, lo que genera la misma distancia para y)  *  por la tangente de angulo 45 que es 1,
            posteriormente multiplicado por la amplitud llamada hurtJumpSpeed en HurtSMB*/

            return new Vector2(damageDirection.x, y).normalized;//es clave normalizar ya que pequeños valores los transforma a escalar 1
        }
        //Si es herido
        public void OnHurt(Damager damager, Damageable damageable)
        {
            //if the player don't have control, we shouldn't be able to be hurt as this wouldn't be fair
            if (!PlayerInput.Instance.HaveControl)
                return;
            //Actualice la cara segun la direccion del daño obtenido
            UpdateFacing(damageable.GetDamageDirection().x > 0f);//si es mayor a cero lanzo el ataque hacia la derecha y me impacto en la izquerda
            damageable.EnableInvulnerability();//activa invulnerabilidad
            //Trigger de daño recibido al animador
            m_Animator.SetTrigger(m_HashHurtPara);//Llama a HurtSMB que activa el flickering

            //solo forzamos la reaparición si helath > 0, de lo contrario, los activadores forceRespawn y Death se establecen en el animador, jugando entre sí.
            //we only force respawn if helath > 0, otherwise both forceRespawn & Death trigger are set in the animator, messing with each other.
            if (damageable.CurrentHealth > 0 && damager.forceRespawn)
                m_Animator.SetTrigger(m_HashForcedRespawnPara);
            //piso falso
            m_Animator.SetBool(m_HashGroundedPara, false);
            hurtAudioPlayer.PlayRandomSound();
            // si la salud es < 0, significa muerte la devolución de llamada tomara cuidado de reaparecer
            //if the health is < 0, mean die callback will take care of respawn
            if (damager.forceRespawn && damageable.CurrentHealth > 0)
            {
                StartCoroutine(DieRespawnCoroutine(false, true));
            }
        }
        //Si muero, es llamado en el evento ondie en Damageable
        public void OnDie()
        {
            m_Animator.SetTrigger(m_HashDeadPara);//envio parametro de muerte

            StartCoroutine(DieRespawnCoroutine(true, false));//e inicio corutina abajo
        }
        //llamado por OnDie aca arriba y tambien llamado en Onhurt
        IEnumerator DieRespawnCoroutine(bool resetHealth, bool useCheckPoint)
        {
            PlayerInput.Instance.ReleaseControl(true);
            yield return new WaitForSeconds(1.0f); //wait one second before respawing
            yield return StartCoroutine(ScreenFader.FadeSceneOut(useCheckPoint ? ScreenFader.FadeType.Black : ScreenFader.FadeType.GameOver));
            if(!useCheckPoint)
                yield return new WaitForSeconds (2f);
            Respawn(resetHealth, useCheckPoint);
            yield return new WaitForEndOfFrame();
            yield return StartCoroutine(ScreenFader.FadeSceneIn());
            PlayerInput.Instance.GainControl();
        }
        //Inice el parapadeo al ser herido, inicia una corrutina y es llamado desde HurtSMB
        public void StartFlickering()
        {
            m_FlickerCoroutine = StartCoroutine(Flicker());//a la variable de tipo Coroutine iniciele una corrutina flicker()
        }
        //Detenga la corutina de parpadeo llamada en Respawn a su vez llamado en OnDie
        public void StopFlickering()
        {
            StopCoroutine(m_FlickerCoroutine);
            spriteRenderer.enabled = true;
        }
        //Comprueba si ataque, si presione el boton, desencadenando el siguiente metodo abajo
        public bool CheckForMeleeAttackInput()//Llamado por Locomotion
        {
            return PlayerInput.Instance.MeleeAttack.Down;
        }
        //Activado por el metodo anterior en locomotion
        public void MeleeAttack()
        {
            m_Animator.SetTrigger(m_HashMeleeAttackPara);//activo parametro de ataque 
        }
        //Habilitado por Script MeleeAttack en Behaviour del estado MeleeAttack al entrar al estado
        public void EnableMeleeAttack()
        {
            meleeDamager.EnableDamage();//Habilita m_CanDamage de Damager
            meleeDamager.disableDamageAfterHit = true;//Deshablite el daño cuerpo a cuerpo una vez lo ejecute, es el unico lugar donde se cambia esta variable a true en el script Damager
            meleeAttackAudioPlayer.PlayRandomSound();
        }
        //Habilitado por Script MeleeAttack en Behaviour del estado MeleeAttack al entrar al estado
        public void DisableMeleeAttack()
        {
            meleeDamager.DisableDamage();//Deshablite el daño cuerpo a cuerpo
        }
        //lleven el collider al piso
        public void TeleportToColliderBottom()
        {   //posicion del rigig + compensacion del collider en si + (0,-1) * tamaño * la mitad
            Vector2 colliderBottom = m_CharacterController2D.Rigidbody2D.position + m_Capsule.offset + Vector2.down * m_Capsule.size.y * 0.5f;
            m_CharacterController2D.Teleport(colliderBottom);//funcion de teletransportar mueve el rigidbody a la posicion deseada
        }

        public void PlayFootstep()
        {
            footstepAudioPlayer.PlayRandomSound(m_CurrentSurface);
            var footstepPosition = transform.position;
            footstepPosition.z -= 1;
            VFXController.Instance.Trigger("DustPuff", footstepPosition, 0, false, null, m_CurrentSurface);
        }
        //Es llamada por DieRespawnCoroutine
        public void Respawn(bool resetHealth, bool useCheckpoint)
        {
            if (resetHealth)
                damageable.SetHealth(damageable.startingHealth);
            // reiniciamos el activador de daño, ya que no queremos que el jugador regrese a la animación de daño una vez reaparecido
            //we reset the hurt trigger, as we don't want the player to go back to hurt animation once respawned
            m_Animator.ResetTrigger(m_HashHurtPara);
            if (m_FlickerCoroutine != null)
            {//we stop flcikering for the same reason
                StopFlickering();
            }

            m_Animator.SetTrigger(m_HashRespawnPara);

            if (useCheckpoint && m_LastCheckpoint != null)
            {
                UpdateFacing(m_LastCheckpoint.respawnFacingLeft);
                GameObjectTeleporter.Teleport(gameObject, m_LastCheckpoint.transform.position);
            }
            else
            {
                UpdateFacing(m_StartingFacingLeft);
                GameObjectTeleporter.Teleport(gameObject, m_StartingPosition);
            }
        }

        public void SetChekpoint(Checkpoint checkpoint)
        {
            m_LastCheckpoint = checkpoint;
        }

        //This is called by the inventory controller on key grab, so it can update the Key UI.
        public void KeyInventoryEvent()
        {
            if (KeyUI.Instance != null) KeyUI.Instance.ChangeKeyUI(m_InventoryController);
        }
    }
}
