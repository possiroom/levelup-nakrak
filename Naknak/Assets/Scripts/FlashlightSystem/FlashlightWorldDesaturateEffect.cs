using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

namespace Naknak.FlashlightSystem
{
    public class FlashlightWorldDesaturateEffect : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float saturation = 0f;
        [SerializeField, Range(0f, 1f)] private float darkness = 0f;
        [SerializeField] private bool includePlayer = false;
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] exclusions = new Transform[0];

        private readonly List<RendererMaterialState> rendererMaterialStates = new List<RendererMaterialState>();
        private Material desaturateMaterial;
        private bool applied;
        private HashSet<Transform> sceneExclusions;
        private Scene? sceneScope;

        private void OnDisable()
        {
            Clear();
        }

        private void LateUpdate()
        {
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (desaturateMaterial == null)
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
            UpdateMaterial();

            Renderer[] renderers = FindObjectsByType<Renderer>(
                sceneScope.HasValue ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
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

        // Scope memory styling to its content scene, including props activated later in a cutscene.
        public void ApplyToScene(Scene scene, IEnumerable<Transform> keepColor)
        {
            Clear();
            sceneScope = scene;
            sceneExclusions = keepColor != null ? new HashSet<Transform>(keepColor) : null;
            Apply();
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
            if (sceneScope.HasValue && targetRenderer.gameObject.scene != sceneScope.Value)
                return false;

            if (targetRenderer.GetComponentInParent<Canvas>() != null)
                return false;

            if (!includePlayer && player != null && targetRenderer.transform.IsChildOf(player))
                return false;

            foreach (var excluded in exclusions)
                if (excluded != null && targetRenderer.transform.IsChildOf(excluded)) return false;
            if (sceneExclusions != null)
                foreach (var excluded in sceneExclusions)
                    if (excluded != null && targetRenderer.transform.IsChildOf(excluded)) return false;

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
