using UnityEngine;
using System.Collections;

namespace AnimatedPixelPack
{
    public class HorizontalProjectile : WeaponProjectile
    {
        // Editor Properties
        [Header("Projectile")]
        public int Speed = 100;
        public int RotationSpeed = 0;

        // Members
        protected Transform RenderTransform { get; private set; }
        protected Vector3 RotationPoint = new Vector3();

        protected override void Start()
        {
            base.Start();

            // Get the sprite from the child components so that we can rotate it even if we have a slider joint
            SpriteRenderer sprite = this.GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
            {
                this.RenderTransform = sprite.transform;
            }

            // Get the slider joint that prevents any Y movement
            SliderJoint2D joint = this.GetComponent<SliderJoint2D>();
            if (joint != null)
            {
                joint.anchor = new Vector2(this.transform.position.x, -this.transform.position.y);
            }

            // Give it some velocity
            float x = DirectionX * this.Speed;
            Rigidbody2D body = this.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.AddForce(new Vector2(x, 0));
            }
        }

        protected override void Update()
        {
            base.Update();

            // If we are a rotating projectile, then rotate the rendering part
            if (this.RotationSpeed != 0 && this.RenderTransform != null)
            {
                this.RenderTransform.RotateAround(this.transform.position + this.RotationPoint, Vector3.forward, Time.deltaTime * -this.RotationSpeed * this.DirectionX);
            }
        }
    }
}