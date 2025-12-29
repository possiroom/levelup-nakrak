using UnityEngine;
using UnityEngine.Tilemaps;

public class StairManager : MonoBehaviour
{
    // Assign the two stair layers in the inspector
    public LayerMask stairLayer1;
    public LayerMask stairLayer2;
    //플레이어
    public PlayerMoveController3 player;

    //계단 트리거에서 빠져나올 시에, 플레이어의 층과 LayerMask를 토글
    private void OnTriggerExit2D(Collider2D other)
    {
        // 1. 나간 물체가 플레이어인지 확인 (태그 또는 컴포넌트 비교)
        // 여기서 other는 플레이어의 콜라이더입니다.
        if (other.gameObject.GetComponent<PlayerMoveController3>() != null)
        {
            if (player == null) return;

            // 2. 현재 플레이어의 레이어 마스크 값(value)을 비교하여 토글
            if (player.floorLayer.value == stairLayer1.value)
            {
                player.floorLayer = stairLayer2;
                player.gameObject.layer = LayerMask.NameToLayer(GetLayerNameFromMask(stairLayer2));
                Debug.Log("층 변경: Layer 2로");
            }
            else if (player.floorLayer.value == stairLayer2.value)
            {
                player.floorLayer = stairLayer1;
                player.gameObject.layer = LayerMask.NameToLayer(GetLayerNameFromMask(stairLayer1));
                Debug.Log("층 변경: Layer 1로");
            }
        }
    }

    // LayerMask로부터 레이어 이름을 추출하는 함수
    private string GetLayerNameFromMask(LayerMask mask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                return LayerMask.LayerToName(i);
            }
        }
        return "Default";
    }
}
