using UnityEngine;
using System;

public interface IFarmAction
{
    // 지금 이 행동을 실행할 수 있는가?
    bool CanExecute(Vector3Int cellPos, PlayerInventory inventory, FarmingManager farm);

    // 행동 실행. facingDir = 플레이어 바라보는 방향 (GOOD/PERFECT 범위 계산에 사용)
    void Execute(Vector3Int cellPos, Vector2Int facingDir, PlayerInventory inventory, FarmingManager farm, Action onComplete);
}
