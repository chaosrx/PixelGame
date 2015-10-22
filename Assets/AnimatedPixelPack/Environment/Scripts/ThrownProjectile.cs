using UnityEngine;
using System.Collections;

namespace AnimatedPixelPack
{
    public class ThrownProjectile : HorizontalProjectile
    {
        // Editor Properties
        [Header("Thrown")]
        public bool IsMainItem = true;
        public float StartRotation = 0f;

        protected override void Start()
        {
            base.Start();
            
            // Get the sprite from the hand of the character
            Sprite weaponSprite = this.GetComponentInChildren<SpriteRenderer>().sprite;
            BoxCollider2D weaponBox = this.GetComponentInChildren<BoxCollider2D>();
            if (this.Owner != null)
            {
                SpriteRenderer[] parts = this.Owner.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].name == (this.IsMainItem ? "MainItem" : "OffItem"))
                    {
                        weaponSprite = parts[i].sprite;
                        weaponBox = parts[i].gameObject.GetComponent<BoxCollider2D>();
                        break;
                    }
                }
            }

            // Update our sprite to match the one we are throwing
            if (weaponSprite != null)
            {
                SpriteRenderer sr = this.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = weaponSprite;
                }
            }

            // Update our collision box based on the one we are throwing
            if (weaponBox != null)
            {
                BoxCollider2D bc = this.GetComponentInChildren<BoxCollider2D>();
                if (bc != null)
                {
                    bc.offset = weaponBox.offset;
                    bc.size = weaponBox.size;
                }

                this.RenderTransform.localPosition = -weaponBox.offset;
                this.RenderTransform.RotateAround(this.transform.position, Vector3.forward, this.StartRotation * this.DirectionX);
            }
        }
    }
}
