using UnityEngine;
using System.Collections;

namespace AnimatedPixelPack
{
    public class SimpleDemoAI : Character
    {
        protected override Vector3 GetMovement(float y)
        {
            // Always walk to the left when on the ground
            Vector3 result = Vector3.zero;
            if (this.isGrounded)
            {
                result = Vector3.left * this.WalkSpeed * Time.deltaTime;
            }

            result.y = y;

            return result;
        }

        protected override void GetAction()
        {
            // Do no action, could be updated to attack when near the player
        }

        protected virtual void OnCollisionEnter2D(Collision2D coll)
        {
            if (coll.collider.tag == "HeldItem")
            {
                // Take some damage if we are attacked
                Character hurtBy = coll.collider.GetComponentInParent<Character>();
                if (hurtBy != null && hurtBy.IsAttacking)
                {
                    // Apply damage to this character
                    float direction = coll.contacts[0].point.x - this.transform.position.x;
                    this.ApplyDamage(100, direction);
                }
            }
        }
    }
}
