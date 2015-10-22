using UnityEngine;
using System.Collections;

namespace AnimatedPixelPack
{
    public abstract class WeaponProjectile : MonoBehaviour
    {
        // Editor Properties
        [Header("Weapon")]
        public int Damage = 50;

        // Members
        protected Character Owner { get; private set; }
        protected int DirectionX { get; private set; }

        /// <summary>
        /// Instantiate a new instance of the WeaponProjectile class using the supplied parameters
        /// </summary>
        /// <param name="instance">The instance to use as the base</param>
        /// <param name="owner">The character that owns this projectile</param>
        /// <param name="launchPoint">Where to spawn the projectile</param>
        /// <param name="directionX">The direction to move</param>
        /// <returns>The new projectile</returns>
        public static WeaponProjectile Create(WeaponProjectile instance, Character owner, Transform launchPoint, int directionX)
        {
            WeaponProjectile projectile = GameObject.Instantiate<WeaponProjectile>(instance);
            projectile.Owner = owner;

            // Prevent hitting the player who cast it
            Collider2D projectileCollider = projectile.GetComponentInChildren<Collider2D>();
            Collider2D[] colliders = owner.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Physics2D.IgnoreCollision(colliders[i], projectileCollider);
            }

            // Set the start position
            Vector2 position = launchPoint.position;
            projectile.transform.position = position;
            projectile.DirectionX = directionX;

            // Flip the sprite if necessary
            if (directionX < 0)
            {
                Vector3 rotation = projectile.transform.localRotation.eulerAngles;
                rotation.y -= 180;
                projectile.transform.localEulerAngles = rotation;

            }

            return projectile;
        }

        protected virtual void Start()
        {
            // Get rid of the projectile after a while if it doesn't hit anything
            StartCoroutine(this.DestroyAfter(3));
        }

        protected virtual void Update()
        {
        }

        protected virtual void OnCollisionEnter2D(Collision2D coll)
        {
            GameObject.Destroy(this.gameObject);

            // Apply damage to any character hit by this projectile
            Character character = coll.transform.GetComponent<Character>();
            if (character != null)
            {
                float direction = coll.contacts[0].point.x - character.transform.position.x;
                character.ApplyDamage(this.Damage, direction);
            }
        }

        protected IEnumerator DestroyAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            GameObject.Destroy(this.gameObject);
        }
    }
}
