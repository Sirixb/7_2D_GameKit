using System;//Libreria necesaria para usar Serializable
using UnityEngine;
using UnityEngine.Events;//Libreria necesaria para usar eventos

namespace Gamekit2D
{   //Hereda de mas clases
    public class Damageable : MonoBehaviour, IDataPersister
    {
        [Serializable]
        public class HealthEvent : UnityEvent<Damageable>
        { }

        [Serializable]
        public class DamageEvent : UnityEvent<Damager, Damageable>
        { }

        [Serializable]
        public class HealEvent : UnityEvent<int, Damageable>
        { }

        public int startingHealth = 5;
        public bool invulnerableAfterDamage = true;//Esta variable no se usa para nada por lo menos no lo he descubierto
        public float invulnerabilityDuration = 3f;
        public bool disableOnDeath = false;
        [Tooltip("An offset from the obejct position used to set from where the distance to the damager is computed")]
        public Vector2 centreOffset = new Vector2(0f, 1f);
        public HealthEvent OnHealthSet;//Evento en el inspector
        public DamageEvent OnTakeDamage;//Evento en el inspector
        public DamageEvent OnDie;//Evento en el inspector
        public HealEvent OnGainHealth;//Evento en el inspector
        [HideInInspector]
        public DataSettings dataSettings;

        protected bool m_Invulnerable;
        protected float m_InulnerabilityTimer;
        protected int m_CurrentHealth;
        protected Vector2 m_DamageDirection;
        protected bool m_ResetHealthOnSceneReload;

        public int CurrentHealth
        {
            get { return m_CurrentHealth; }
        }

        void OnEnable()
        {
            PersistentDataManager.RegisterPersister(this);
            m_CurrentHealth = startingHealth;

            OnHealthSet.Invoke(this);

            DisableInvulnerability();
        }

        void OnDisable()
        {
            PersistentDataManager.UnregisterPersister(this);
        }

        void Update()
        {   //si es invulnerable porque fue herido, temporizador invulnerable lo configura EnableInvulnerability abajo
            if (m_Invulnerable)
            {
                m_InulnerabilityTimer -= Time.deltaTime;

                if (m_InulnerabilityTimer <= 0f)
                {
                    m_Invulnerable = false;
                }
            }
        }
        //Habilitar Invulnerabilidad tiene efecto arriba
        public void EnableInvulnerability(bool ignoreTimer = false)
        {
            m_Invulnerable = true;
            //técnicamente no ignore el temporizador, solo configúrelo en un número increíblemente grande. Permitir evitar agregar más pruebas y caso especial.
            //technically don't ignore timer, just set it to an insanly big number. Allow to avoid to add more test & special case.
            m_InulnerabilityTimer = ignoreTimer ? float.MaxValue : invulnerabilityDuration;
        }

        public void DisableInvulnerability()
        {
            m_Invulnerable = false;
        }
        //obtiene la direccion del daño
        public Vector2 GetDamageDirection()
        {
            return m_DamageDirection;//es configurada abajo
        }
        //Tomar dañollamado desde el script Damager
        public void TakeDamage(Damager damager, bool ignoreInvincible = false)
        {   //si es invulnerable y no esta en ignorar invencible o  la sangre es menor a cero retorne el codigo
            if ((m_Invulnerable && !ignoreInvincible) || m_CurrentHealth <= 0)
                return;
            //podemos encontrar ese punto si el damager fue uno que estaba ignorando el estado invencible
            //we can reach that point if the damager was one that was ignoring invincible state.
            //Todavía queremos la devolución de llamada que golpeo, pero no el daño que se eliminará de la salud.
            //We still want the callback that we were hit, but not the damage to be removed from health.

            if (!m_Invulnerable)
            {
                m_CurrentHealth -= damager.damage;//resta el daño
                OnHealthSet.Invoke(this);//configura dos metodos en el canvas
            }
            //Direccion Daño= la posicion actual + vector 3 * centreOffset: 0 , 1 lo que hace que se posicione mas al centro - la posicion del dañador 
            m_DamageDirection = transform.position + (Vector3)centreOffset - damager.transform.position;

            OnTakeDamage.Invoke(damager, this);//Este evento llama al script y metodo deseado en el inspector para el jugador llama OnHurt en el script PlayerCharacter para el enemigo llama EnemyBehaviour.Hit
            //Si muere
            if (m_CurrentHealth <= 0)
            {
                OnDie.Invoke(damager, this);//evento ondie que llama a ondie en PlayerCharacter
                m_ResetHealthOnSceneReload = true;
                EnableInvulnerability();
                if (disableOnDeath) gameObject.SetActive(false);
            }
        }
        //Ganar Sangre
        public void GainHealth(int amount)
        {
            m_CurrentHealth += amount;

            if (m_CurrentHealth > startingHealth)
                m_CurrentHealth = startingHealth;

            OnHealthSet.Invoke(this);

            OnGainHealth.Invoke(amount, this);
        }
        //Establecer Sangre
        public void SetHealth(int amount)
        {
            m_CurrentHealth = amount;

            if (m_CurrentHealth <= 0)
            {
                OnDie.Invoke(null, this);
                m_ResetHealthOnSceneReload = true;
                EnableInvulnerability();
                if (disableOnDeath) gameObject.SetActive(false);
            }

            OnHealthSet.Invoke(this);
        }

        public DataSettings GetDataSettings()
        {
            return dataSettings;
        }

        public void SetDataSettings(string dataTag, DataSettings.PersistenceType persistenceType)
        {
            dataSettings.dataTag = dataTag;
            dataSettings.persistenceType = persistenceType;
        }

        public Data SaveData()
        {
            return new Data<int, bool>(CurrentHealth, m_ResetHealthOnSceneReload);
        }

        public void LoadData(Data data)
        {
            Data<int, bool> healthData = (Data<int, bool>)data;
            m_CurrentHealth = healthData.value1 ? startingHealth : healthData.value0;
            OnHealthSet.Invoke(this);
        }


    }
}
