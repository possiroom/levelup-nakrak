using System.Collections.Generic;
using UnityEngine;

namespace Naknak.FlashlightSystem
{
    public class FlashlightOccluder : MonoBehaviour
    {
        private static readonly List<FlashlightOccluder> ActiveOccluders = new List<FlashlightOccluder>();

        [SerializeField] private bool blocksVision = true;
        [SerializeField] private bool useChildrenBounds = true;
        [SerializeField] private Vector2 boundsPadding = Vector2.zero;

        public static IReadOnlyList<FlashlightOccluder> All => ActiveOccluders;
        public bool BlocksVision => blocksVision && isActiveAndEnabled;

        private void OnEnable()
        {
            if (!ActiveOccluders.Contains(this))
                ActiveOccluders.Add(this);
        }

        private void OnDisable()
        {
            ActiveOccluders.Remove(this);
        }

        public Bounds GetWorldBounds()
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(transform.position, Vector3.zero);

            Collider2D ownCollider = GetComponent<Collider2D>();
            if (ownCollider != null)
            {
                bounds = ownCollider.bounds;
                hasBounds = true;
            }

            Renderer ownRenderer = GetComponent<Renderer>();
            if (ownRenderer != null)
            {
                if (hasBounds) bounds.Encapsulate(ownRenderer.bounds);
                else
                {
                    bounds = ownRenderer.bounds;
                    hasBounds = true;
                }
            }

            if (useChildrenBounds)
            {
                Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();
                foreach (Collider2D childCollider in childColliders)
                {
                    if (hasBounds) bounds.Encapsulate(childCollider.bounds);
                    else
                    {
                        bounds = childCollider.bounds;
                        hasBounds = true;
                    }
                }

                Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer childRenderer in childRenderers)
                {
                    if (hasBounds) bounds.Encapsulate(childRenderer.bounds);
                    else
                    {
                        bounds = childRenderer.bounds;
                        hasBounds = true;
                    }
                }
            }

            if (!hasBounds)
                bounds = new Bounds(transform.position, Vector3.one);

            bounds.Expand(new Vector3(boundsPadding.x * 2f, boundsPadding.y * 2f, 0f));
            return bounds;
        }
    }
}
