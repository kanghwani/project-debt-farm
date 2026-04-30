using UnityEngine;

/// <summary>
/// 드론 상자 오브젝트에 붙이는 트리거.
/// 플레이어가 접근하면 DroneShop UI를 열고, 벗어나면 닫는다.
///
/// 세팅:
///   1. 이 스크립트를 드론 상자 GameObject에 추가
///   2. Collider2D 추가 → Is Trigger 체크
///   3. Inspector: Drone Shop → DroneShop 컴포넌트 연결
///   4. Inspector: Player Layer → Player 레이어 선택
/// </summary>
public class DroneShopTrigger : MonoBehaviour
{
    [Tooltip("씬의 DroneShop 컴포넌트를 연결하세요")]
    [SerializeField] private DroneShop droneShop;

    [Tooltip("Player 레이어 선택")]
    [SerializeField] private LayerMask playerLayer;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        droneShop?.OpenShop();
        AudioManager.PlaySFX(SfxType.ShopOpen);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        droneShop?.CloseShop();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        if (TryGetComponent(out Collider2D col))
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
