using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Naknak.FlashlightSystem
{
    public class FlashlightWorldDesaturateEffect : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float saturation = 0f;
        [SerializeField, Range(0f, 1f)] private float darkness = 0f;
        [SerializeField] private bool includePlayer = false;
        [SerializeField] private Transform player;

        private readonly List<RendererMaterialState> rendererMaterialStates = new List<RendererMaterialState>();
        private Material desaturateMaterial;
        private bool applied;

        private void OnDisable()
        {
            Clear();
        }

        private void LateUpdate()
        {
            if (!applied || desaturateMaterial == null)
                return;

            Vector3 center = player != null ? player.position : Vector3.zero;
            desaturateMaterial.SetVector("_VisionCenter", new Vector4(center.x, center.y, center.z, 0f));
            desaturateMaterial.SetFloat("_Radius", -1f);
            desaturateMaterial.SetFloat("_Softness", 0.01f);
            desaturateMaterial.SetFloat("_OutsideSaturation", saturation);
            desaturateMaterial.SetFloat("_OutsideDarkness", darkness);
            desaturateMaterial.SetFloat("_NoisePower", 0f);
            desaturateMaterial.SetFloat("_NoiseScale", 1f);
            desaturateMaterial.SetFloat("_TimeValue", Time.time);
        }

        public void Apply()
        {
            if (applied)
                return;

            ResolvePlayer();

            Shader shader = Shader.Find("Custom/VisionDesaturateWorld");
            if (shader == null)
            {
                Debug.LogWarning("[FlashlightWorldDesaturateEffect] Custom/VisionDesaturateWorld shader not found.");
                return;
            }

            desaturateMaterial = new Material(shader);

            Renderer[] renderers = FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (Renderer targetRenderer in renderers)
            {
                if (!CanApply(targetRenderer))
                    continue;

                Material[] originalMaterials = targetRenderer.sharedMaterials;
                Material[] effectMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < effectMaterials.Length; i++)
                    effectMaterials[i] = desaturateMaterial;

                rendererMaterialStates.Add(new RendererMaterialState(targetRenderer, originalMaterials));
                targetRenderer.sharedMaterials = effectMaterials;
            }

            applied = true;
        }

        public void Clear()
        {
            if (!applied)
                return;

            foreach (RendererMaterialState state in rendererMaterialStates)
            {
                if (state.Renderer != null)
                    state.Renderer.sharedMaterials = state.Materials;
            }

            rendererMaterialStates.Clear();

            if (desaturateMaterial != null)
            {
                Destroy(desaturateMaterial);
                desaturateMaterial = null;
            }

            applied = false;
        }

        private void ResolvePlayer()
        {
            if (player != null)
                return;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        private bool CanApply(Renderer targetRenderer)
        {
            if (targetRenderer == null)
                return false;

            if (targetRenderer.GetComponentInParent<Canvas>() != null)
                return false;

            if (!includePlayer && player != null && targetRenderer.transform.IsChildOf(player))
                return false;

            return targetRenderer is SpriteRenderer || targetRenderer is TilemapRenderer;
        }

        private readonly struct RendererMaterialState
        {
            public RendererMaterialState(Renderer renderer, Material[] materials)
            {
                Renderer = renderer;
                Materials = materials;
            }

            public Renderer Renderer { get; }
            public Material[] Materials { get; }
        }
    }
}
