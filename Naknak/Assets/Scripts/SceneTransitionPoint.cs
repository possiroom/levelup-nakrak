using UnityEngine;

public sealed class SceneTransitionPoint : MonoBehaviour, IInteractable
{
    public SceneFlowContainer container;
    [HideInInspector] public string connectionId;
    public bool enterOnTrigger;

    public void Interact()
    {
        if (container == null) return;
        var connection = container.connections.Find(c => c != null && c.id == connectionId);
        if (connection != null) SceneLoadManager.Instance?.Travel(container, connection);
    }

    private void OnTriggerEnter2D(Collider2D other)
    { if (enterOnTrigger && other.CompareTag("Player")) Interact(); }
    private void OnTriggerEnter(Collider other)
    { if (enterOnTrigger && other.CompareTag("Player")) Interact(); }
    private void OnDrawGizmos()
    { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, 0.3f); }
}
