using UnityEngine;
using UnityEngine.Tilemaps;

public class ColiderManager : MonoBehaviour
{
    TilemapCollider2D colider;
    public LayerMask floor;  // Inspector에서 이 타일맵이 어느 층인지 설정
    public PlayerMoveController3 player;
    void Start()
    {
        colider = GetComponent<TilemapCollider2D>();
        // 초기 상태 설정
        UpdateColliderState();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateColliderState();
    }

    private void UpdateColliderState()
    {
        if (player == null) return;
        
        // 플레이어의 floorLayer와 이 타일의 floor가 같으면 collider 끄기
        if (player.floorLayer.value == floor.value)
        {
            colider.enabled = true;
        }
        else
        {
            colider.enabled = false;
        }
    }
}
