using UnityEngine;
using UnityEngine.Rendering;

/// <summary>People and whole props share a depth order measured at their feet.</summary>
[ExecuteAlways, DefaultExecutionOrder(10000)]
public sealed class WorldDepthSort : MonoBehaviour
{
    [SerializeField] private bool useSpriteBottom = true;
    private SpriteRenderer sprite;
    private SortingGroup group;

    public float GroundY => useSpriteBottom && sprite != null ? sprite.bounds.min.y : transform.position.y;

    public static void EnsureActor(Transform actor)
    {
        if (actor != null && actor.GetComponent<WorldDepthSort>() == null)
            actor.gameObject.AddComponent<WorldDepthSort>();
    }

    private void OnEnable() { sprite = GetComponent<SpriteRenderer>(); group = GetComponent<SortingGroup>(); Refresh(); }
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        int order = Mathf.Clamp(Mathf.RoundToInt(-GroundY * 100f), -30000, 30000);
        if (group != null)
        {
            group.sortingLayerName = "Foreground";
            group.sortingOrder = order;
        }
        else if (sprite != null)
        {
            sprite.sortingLayerName = "Foreground";
            sprite.sortingOrder = order + 1;
        }
    }
}
