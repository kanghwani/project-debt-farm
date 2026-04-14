using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShippingBox : MonoBehaviour
{
    [Header("Settings")] 
    [Tooltip("플레이어 레이어 선택")] 
    [SerializeField] private LayerMask playerLayer;

    private bool isPlayerNearby = false;
    private PlayerInventory playerInventory;
    private PlayerInputReader playerInput;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            // 인벤토리와 리더기를 둘 다 가져옵니다.
            if (collision.TryGetComponent(out PlayerInventory inv) && 
                collision.TryGetComponent(out PlayerInputReader input))
            {
                isPlayerNearby = true;
                playerInventory = inv;
                playerInput = input;

                // 
                playerInput.OnInteractEvent += HandleShipping;
                Debug.Log("🛸 드론 상자: 상호작용 가능 (Space)");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            isPlayerNearby = false;
            
            // 
            if (playerInput != null)
            {
                playerInput.OnInteractEvent -= HandleShipping;
                playerInput = null;
            }
            playerInventory = null;
        }
    }
    
    private void HandleShipping()
    {
        if (isPlayerNearby && playerInventory != null)
        {
            if (playerInventory.currentWeight > 0)
            {
                playerInventory.ShipAllItems();
            }
            else
            {
                Debug.Log("🛸 드론 상자: 보낼 물건이 없습니다.");
            }
        }
    }

    private void Update()
    {
        if (isPlayerNearby && playerInventory != null)
        {
            if (Keyboard.current.digit8Key.wasPressedThisFrame) 
                playerInventory.BuySeed(TileData.Crops.radish, 50); // 무 씨앗 50G
        
            if (Keyboard.current.digit9Key.wasPressedThisFrame) 
                playerInventory.BuySeed(TileData.Crops.potato, 120); // 감자 씨앗 120G
        }
    }
}