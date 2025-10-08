using UnityEngine;
using UnityEngine.Tilemaps;

public class HideColliders : MonoBehaviour
{
    TilemapRenderer tilemapRenderer;
    void Start()
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemapRenderer.enabled = false;
    }
}
