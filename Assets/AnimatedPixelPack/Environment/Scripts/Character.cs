using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AnimatedPixelPack
{
    [RequireComponent(typeof(Animator))]
    public class Character : MonoBehaviour
    {
        // Editor Properties
        [Header("Character")]
        public Transform GroundChecker;
        public LayerMask GroundLayer;
        public float WalkSpeed = 60;
        public float RunSpeed = 120;
        public float JumpPower = 300;
        public float RunningJumpPower = 450;
        public int MaxHealth = 100;
        public float GravityScale = 2;
        public bool IsZombified = false;

        [Header("Weapon")]
        public WeaponType EquippedWeaponType;
        public bool IsBlockEnabled = false;
        public Transform LaunchPoint;
        public WeaponProjectile CastProjectile;
        public WeaponProjectile ThrowMainProjectile;
        public WeaponProjectile ThrowOffProjectile;
        public Transform EffectPoint;
        public WeaponEffect Effect;

        // Script Properties
        public int CurrentHealth { get; private set; }
        public bool IsDead { get { return this.CurrentHealth <= 0; } }
        public Direction CurrentDirection { get; private set; }
        public bool IsAttacking
        {
            get
            {
                AnimatorStateInfo state = this.animatorObject.GetCurrentAnimatorStateInfo(0);
                return state.IsName("Attack") || state.IsName("Quick Attack");
            }
        }

        public enum WeaponType
        {
            None = 0,
            Staff = 1,
            Sword = 2,
            Bow = 3,
            Gun = 4
        }

        public enum Direction
        {
            Left = -1,
            Right = 1
        }

        // Members
        protected Rigidbody body;
        protected Rigidbody2D body2D;
        protected bool isGrounded = true;
        private Animator animatorObject;
        private bool isRunning = false;
        private float groundRadius = 0.1f;
        private WeaponEffect activeEffect;
        private Direction startDirection = Direction.Right;

        /// <summary>
        /// Instantiate a new character with the supplied parameters
        /// </summary>
        /// <param name="instance">The instance to use as the base</param>
        /// <param name="startDirection">Direction the new character should be facing</param>
        /// <param name="position">The position to spawn at</param>
        /// <returns>The new character</returns>
        public static Character Create(Character instance, Direction startDirection, Vector3 position)
        {
            Character c = GameObject.Instantiate<Character>(instance);
            c.transform.position = position;
            c.startDirection = startDirection;
            return c;
        }

        protected virtual void Start()
        {
            // Grab the editor objects
            this.body = this.GetComponent<Rigidbody>();
            this.body2D = this.GetComponent<Rigidbody2D>();
            this.animatorObject = this.GetComponent<Animator>();

            // Apply the gravity scale because 2D physics jumping look too floaty without extra gravity
            if (this.body2D != null)
            {
                this.body2D.gravityScale = this.GravityScale;
            }

            // Setup the character
            this.CurrentHealth = this.MaxHealth;
            this.ApplyDamage(0);
            this.body2D.centerOfMass = new Vector2(0f, 0.4f);
            if (this.startDirection != Direction.Right)
            {
                this.ChangeDirection(this.startDirection);
            }
            else
            {
                this.CurrentDirection = this.startDirection;
            }
        }

        void FixedUpdate()
        {
            if (this.animatorObject != null)
            {
                // Check if we are touching the ground using the rigidbody (we support both 2d and 3d)
                if (this.body2D != null)
                {
                    this.isGrounded = Physics2D.OverlapCircle(GroundChecker.position, this.groundRadius, this.GroundLayer);
                    this.animatorObject.SetFloat("VelocityY", this.body2D.velocity.y);
                }
                else
                {
                    this.isGrounded = Physics.OverlapSphere(GroundChecker.position, this.groundRadius, this.GroundLayer).Length > 0;
                    this.animatorObject.SetFloat("VelocityY", this.body.velocity.y);
                }

                // Update the animator
                this.animatorObject.SetBool("IsGrounded", this.isGrounded);
                this.animatorObject.SetInteger("WeaponType", (int)this.EquippedWeaponType);
                this.animatorObject.SetBool("IsZombified", this.IsZombified);

                AnimatorStateInfo state = this.animatorObject.GetCurrentAnimatorStateInfo(0);

                if (state.IsName("Stopped") ||
                    state.IsName("Idle") ||
                    state.IsName("WalkAndRun") ||
                    state.IsName("WalkAndRun_Normal") ||
                    state.IsName("WalkAndRun_Zombied") ||
                    state.IsName("JumpAndFall"))
                {
                    // Get the movement vector
                    // By default this will use the input, but the AI dervived class shows how you can override that
                    float y = (this.body2D != null ? this.body2D.velocity.y : this.body.velocity.y);
                    Vector3 movement = this.GetMovement(y);

                    // Update the velocity
                    if (this.body2D != null)
                    {
                        this.body2D.velocity = new Vector2(movement.x, movement.y);
                    }
                    else
                    {
                        // For 3d movement we attempt to predict a collision to stop wall hugging
                        Vector3 test = new Vector3(movement.x, 0, movement.z);
                        float distance = test.magnitude * Time.fixedDeltaTime;
                        test.Normalize();

                        RaycastHit hit;
                        if (this.body.SweepTest(test, out hit, distance) && hit.collider.tag == "Ground")
                        {
                            this.body.velocity = new Vector3(0, movement.y, 0);
                        }
                        else
                        {
                            this.body.velocity = movement;
                        }
                    }

                    // Set the animator property for walking speed
                    this.animatorObject.SetFloat("VelocityWalk", Mathf.Abs(movement.magnitude));

                    // Flip the sprites if necessary
                    if (!Mathf.Approximately(movement.x, 0))
                    {
                        this.ChangeDirection(movement.x < 0 ? Direction.Left : Direction.Right);
                    }
                }
            }
        }

        void Update()
        {
            // Check for keyboard input for the different actions
            // But only when we are on the ground
            if (this.isGrounded && !this.IsDead)
            {
                this.GetAction();
            }
        }

        /// <summary>
        /// Reduce the health of the character by the specified amount
        /// </summary>
        /// <param name="damage">The amount of damage to apply</param>
        /// <param name="direction">The direction that the damage came from (left < 0 > right)</param>
        /// <returns>True if the character dies from this damage, False if it remains alive</returns>
        public bool ApplyDamage(int damage, float direction = 0)
        {
            if (!this.IsDead)
            {
                this.animatorObject.SetFloat("LastHitDirection", direction * (int)this.CurrentDirection);

                // Update the health
                this.CurrentHealth = Mathf.Clamp(this.CurrentHealth - damage, 0, this.MaxHealth);
                this.animatorObject.SetInteger("Health", this.CurrentHealth);

                if (damage != 0)
                {
                    // Show the hurt animation
                    this.TriggerAction("TriggerHurt", false);
                }

                if (this.CurrentHealth <= 0)
                {
                    // Since the player is dead, remove the corpse
                    StartCoroutine(this.DestroyAfter(1));
                }
            }

            return this.IsDead;
        }

        protected virtual Vector3 GetMovement(float y)
        {
            // Get the input and speed
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = (this.body2D != null ? 0 : Input.GetAxis("Vertical"));
            float speed = (this.isRunning ? this.RunSpeed : this.WalkSpeed);

            // Limit diagonal movement speed
            Vector3 result = new Vector3(horizontal, 0, vertical);
            if (result.sqrMagnitude > 1)
            {
                result.Normalize();
            }

            result *= speed * Time.deltaTime;
            result.y = y;

            return result;
        }

        protected virtual void GetAction()
        {
            // Check if we are blocking
            this.animatorObject.SetBool("IsBlocking", Input.GetKey(KeyCode.B) && this.IsBlockEnabled);

            // Check for the running modifier key
            this.isRunning = Input.GetKey(KeyCode.LeftShift);

            // Now check the rest of the keys for actions
            if (Input.GetButtonDown("Jump"))
            {
                float power = (this.isRunning ? this.RunningJumpPower : this.JumpPower);

                if (this.body2D != null)
                {
                    this.body2D.AddForce(new Vector2(0, power));
                }
                else
                {
                    this.body.AddForce(new Vector3(0, power, 0));
                }
            }
            else if (Input.GetButtonDown("Fire1"))
            {
                this.TriggerAction("TriggerQuickAttack");
            }
            else if (Input.GetButtonDown("Fire2"))
            {
                this.TriggerAction("TriggerAttack");
            }
            else if (Input.GetButtonDown("Fire3"))
            {
                this.TriggerAction("TriggerCast");
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                this.TriggerAction("TriggerThrowOff");
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                this.TriggerAction("TriggerThrowMain");
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                this.TriggerAction("TriggerConsume");
            }
            else if (Input.GetKeyDown(KeyCode.B) && this.IsBlockEnabled)
            {
                this.TriggerAction("TriggerBlock");
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                // Apply some damage to test the animation
                this.ApplyDamage(10);
            }
        }

        protected void TriggerAction(string action, bool isCombatAction = true)
        {
            // Update the animator object
            this.animatorObject.SetTrigger(action);
            this.animatorObject.SetBool("IsBlocking", Input.GetKey(KeyCode.B) && this.IsBlockEnabled);

            if (isCombatAction)
            {
                // Combat actions also trigger an additional parameter to move correctly through states
                this.animatorObject.SetTrigger("TriggerCombatAction");
            }

            // Stop the character from moving while we do the animation
            if (this.body2D != null)
            {
                this.body2D.velocity = new Vector3(0, this.body2D.velocity.y, 0);
            }
            else
            {
                this.body.velocity = new Vector3(0, this.body.velocity.y, 0);
            }
        }

        private void ChangeDirection(Direction newDirection)
        {
            if (this.CurrentDirection == newDirection)
            {
                return;
            }

            // Swap the direction of the sprites
            Vector3 rotation = this.transform.localRotation.eulerAngles;
            rotation.y -= 180;
            this.transform.localEulerAngles = rotation;
            this.CurrentDirection = newDirection;

            SpriteRenderer[] sprites = this.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < sprites.Length; i++)
            {
                Vector3 position = sprites[i].transform.localPosition;
                position.z *= -1;
                sprites[i].transform.localPosition = position;
            }
        }

        private void OnCastEffect()
        {
            // If we have an effect start it now
            if (this.Effect != null)
            {
                this.activeEffect = WeaponEffect.Create(this.Effect, this.EffectPoint);
            }
        }
        private void OnCastEffectStop()
        {
            // If we have an effect stop it now
            if (this.activeEffect != null)
            {
                this.activeEffect.Stop();
                this.activeEffect = null;
            }
        }

        private void OnCastComplete()
        {
            // Stop the active effect once we cast
            this.OnCastEffectStop();

            // Create the projectile
            this.LaunchProjectile(this.CastProjectile);
        }

        private void OnThrowMainComplete()
        {
            // Create the projectile for the main hand
            this.LaunchProjectile(this.ThrowMainProjectile);
        }

        private void OnThrowOffComplete()
        {
            // Create the projectile for the off hand
            this.LaunchProjectile(this.ThrowOffProjectile);
        }

        private void LaunchProjectile(WeaponProjectile projectile)
        {
            // Create the projectile
            if (projectile != null)
            {
                WeaponProjectile.Create(
                    projectile,
                    this,
                    this.LaunchPoint,
                    (this.CurrentDirection == Direction.Left ? -1 : 1));
            }
        }

        private IEnumerator DestroyAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            GameObject.Destroy(this.gameObject);
        }
    }
}