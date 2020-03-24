using System;
using UnityEngine;
using UnityEngine.Events;

namespace Gamekit2D
{
    public class Damager : MonoBehaviour
    {
        [Serializable]
        public class DamagableEvent : UnityEvent<Damager, Damageable>
        { }


        [Serializable]
        public class NonDamagableEvent : UnityEvent<Damager>
        { }
        // llama a eso desde dentro de onDamageableHIt o OnNonDamageableHit para obtener lo que fue golpeado.
        //call that from inside the onDamageableHIt or OnNonDamageableHit to get what was hit.
        public Collider2D LastHit { get { return m_LastHit; } }

        public int damage = 1;
        public Vector2 offset = new Vector2(1.5f, 1f);//Sirve para construir el area de la mordida
        public Vector2 size = new Vector2(2.5f, 1f);//Sirve para construir el area de la mordida
        //"Si esto está configurado, el desplazamiento x cambiará de acuerdo con la configuración de flipX del sprite. Por ejemplo, permita que el dañador avance siempre en la dirección del sprite"
        [Tooltip("If this is set, the offset x will be changed base on the sprite flipX setting. e.g. Allow to make the damager alway forward in the direction of sprite")]
        public bool offsetBasedOnSpriteFacing = true;
        [Tooltip("SpriteRenderer used to read the flipX value used by offset Based OnSprite Facing")]
        public SpriteRenderer spriteRenderer;
        [Tooltip("If disabled, damager ignore trigger when casting for damage")]
        public bool canHitTriggers;
        public bool disableDamageAfterHit = false;//Esta variable no aparece en el inspector nose porque supongo porque el script DamagerEditor no lo serializa y sobreescribe el inspector
        [Tooltip("If set, the player will be forced to respawn to latest checkpoint in addition to loosing life")]
        public bool forceRespawn = false;
        //Si se establece, un golpe invencible dañable seguirá recibiendo el mensaje onHit(pero no perderá ninguna vida)
        [Tooltip("If set, an invincible damageable hit will still get the onHit message (but won't loose any life)")]
        public bool ignoreInvincibility = false;
        public LayerMask hittableLayers;
        public DamagableEvent OnDamageableHit;//DamageableEvent pertenece a una clase creada mas arriba aparece en el inspector
        public NonDamagableEvent OnNonDamageableHit;//NonDamageableEvent pertenece a una clase creada mas arriba aparece en el inspector

        protected bool m_SpriteOriginallyFlipped;
        protected bool m_CanDamage = true;
        protected ContactFilter2D m_AttackContactFilter;
        protected Collider2D[] m_AttackOverlapResults = new Collider2D[10];
        protected Transform m_DamagerTransform;
        protected Collider2D m_LastHit;

        void Awake()
        {   //Condiciones del ContacFilter2D
            m_AttackContactFilter.layerMask = hittableLayers;
            m_AttackContactFilter.useLayerMask = true;
            m_AttackContactFilter.useTriggers = canHitTriggers;

            if (offsetBasedOnSpriteFacing && spriteRenderer != null)
                m_SpriteOriginallyFlipped = spriteRenderer.flipX;

            m_DamagerTransform = transform;
        }
        //Llamado desde el metodo StarAtack en EnemyBehaviour a su vez llamado en el evento de animación de ataque / tambien desde PlayerCharacter EnableMeleeAttack()
        public void EnableDamage()
        {
            m_CanDamage = true;//se usa mas abajo para retornar el codigo y poder dañar
        }
        //Llamado desde el metodo EndAtack en EnemyBehaviour a su vez llamado en el evento de animación de ataque
        public void DisableDamage()
        {
            m_CanDamage = false;//se usa mas abajo para retornar el codigo y evitar dañar
        }

        void FixedUpdate()
        {   //si no puede hacer daño retorne la ejecución
            if (!m_CanDamage)// lo activan las funciones de arriba
                return;
            
            //Escala
            Vector2 scale = m_DamagerTransform.lossyScale;
            //Cara de compensacion
            Vector2 facingOffset = Vector2.Scale(offset, scale);
            //si la compensacion basada en la cara y el sprite render no esta vacio y el sprite en X es diferente al sprite original
            if (offsetBasedOnSpriteFacing && spriteRenderer != null && spriteRenderer.flipX != m_SpriteOriginallyFlipped)
                facingOffset = new Vector2(-offset.x * scale.x, offset.y * scale.y);
            print("CanDamage");
            Vector2 scaledSize = Vector2.Scale(size, scale);

            Vector2 pointA = (Vector2)m_DamagerTransform.position + facingOffset - scaledSize * 0.5f;
            Vector2 pointB = pointA + scaledSize;
            //overlap que detecta lo golpeado
            int hitCount = Physics2D.OverlapArea(pointA, pointB, m_AttackContactFilter, m_AttackOverlapResults);
            print(hitCount);
            for (int i = 0; i < hitCount; i++)
            {   //ultima cosa golpeada
                m_LastHit = m_AttackOverlapResults[i];
                Damageable damageable = m_LastHit.GetComponent<Damageable>();//en una variable de damageable que guarde el collider que contenga el componente damageable
                print("ciclo");
                //si lo ultimo golpeado contiene un script damageable adjunto
                if (damageable)
                {
                    print("Damageable");
                    //en OnDamageablethit (evento en el editor) invocamos mandando los dos parametros que recibe de este scrit y el damageable script
                    OnDamageableHit.Invoke(this, damageable);//Evento sin asignacion NOSE que ocurre 
                    damageable.TakeDamage(this, ignoreInvincibility);// y de damageable llamamos a recibir daño enviandole este script y el ignoreInviciility
                    if (disableDamageAfterHit)//solo es cambiada a true por PlayerCharacer.EnableMeleeAttack() a su vez activado por el estado de ataque del jugador     
                        DisableDamage();//deshabilite el daño poniendo en false la variable arriba m_CanDamage
                }
                else
                {   //en OnDamageablethit (evento en el editor) invocamos mandando los dos parametros que recibe de este scrit y el damageable script
                    OnNonDamageableHit.Invoke(this);//No tiene asignación
                }
            }
        }
    }
}
