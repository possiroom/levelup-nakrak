using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>One persistent UI input owner for both gameplay and menu canvases.</summary>
[RequireComponent(typeof(EventSystem))]
public sealed class MenuEventSystemOwner : MonoBehaviour
{
    private EventSystem ownSystem;

    private void OnEnable()
    {
        ownSystem = GetComponent<EventSystem>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSingle();
    }

    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { EnsureSingle(); }

    public void EnsureSingle()
    {
        if (ownSystem == null) ownSystem = GetComponent<EventSystem>();
        foreach (var system in FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (system != ownSystem) system.enabled = false;
        ownSystem.enabled = true;
        EventSystem.current = ownSystem;
    }
}
