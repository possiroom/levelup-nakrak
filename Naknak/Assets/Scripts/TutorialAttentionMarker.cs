using UnityEngine;

/// <summary>Uses the same attention icon as the T0 investigation points.</summary>
public sealed class TutorialAttentionMarker : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private ConditionID hideWhenSet;
    [SerializeField] private Sprite icon;
    private SpriteRenderer marker;

    private void Start()
    {
        if (target == null || icon == null) return;
        var owner = new GameObject("Attention !");
        owner.transform.SetParent(transform, false);
        marker = owner.AddComponent<SpriteRenderer>();
        marker.sprite = icon;
        marker.color = new Color(1f, 0.85f, 0.35f);
        marker.sortingOrder = 200;
    }

    private void LateUpdate()
    {
        if (marker == null || target == null) return;
        marker.gameObject.SetActive(target.gameObject.activeInHierarchy &&
            (hideWhenSet == null || ConditionManager.Instance == null || ConditionManager.Instance.Get(hideWhenSet) == 0));
        var sprite = target.GetComponentInChildren<SpriteRenderer>();
        float top = sprite != null ? sprite.bounds.max.y : target.position.y + 0.7f;
        marker.transform.position = new Vector3(target.position.x, top + 0.45f, target.position.z);
    }
}
