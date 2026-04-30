using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CropStateEngine : MonoBehaviour
{
    private FarmingManager farm;

    private void Start()
    {
        farm = FarmingManager.Instance; // 매니저 연결
    }

    private void Update()
    {
        if (farm == null || farm.activeCrops.Count == 0) return;

        for (int i = farm.activeCrops.Count - 1; i >= 0; i--)
        {
            Vector3Int pos = farm.activeCrops[i];

            if (farm.farmData.TryGetValue(pos, out TileData data))
            {
                // GrowthAccelerator 적용 시 타이머 2배 진행 (성장 시간 50% 단축)
                float timerMult = data.appliedFertilizers.Contains(Fertilizer.GrowthAccelerator)
                    ? 2f : 1f;
                data.currentTimer += Time.deltaTime * timerMult;
                CheckCropState(pos, data);
            }
        }
    }

    // 임시 디버그: 현재 activeCrops 상태 출력 (F5로 호출)
    private void LateUpdate()
    {
        if (!Keyboard.current.tabKey.wasPressedThisFrame) return;
        Debug.Log($"[CropStateEngine] activeCrops: {farm.activeCrops.Count}개");
        foreach (var pos in farm.activeCrops)
        {
            if (farm.farmData.TryGetValue(pos, out TileData d))
                Debug.Log($"  └ {pos} | 상태:{d.currentState} | 물:{d.isWatered} | 타이머:{d.currentTimer:F1}s");
        }
    }

    private void CheckCropState(Vector3Int pos, TileData data)
    {
        CropData cropInfo = DataManager.Instance.GetCrop(data.cropType);
        if (cropInfo == null)
        {
            Debug.LogWarning($"[CropStateEngine] {data.cropType} CropData 없음 — DataManager 인스펙터 확인!");
            return;
        }

        // ── 성장 완료 ────────────────────────────────────────────────────────
        if (data.currentState == TileData.TileState.Seeded && data.currentTimer >= cropInfo.requireGrowTime)
        {
            data.currentState = TileData.TileState.Harvestable;
            data.currentTimer = 0f;
            farm.UpdateTileVisual(pos, cropInfo.harvestableTile);
            AudioManager.PlaySFX(SfxType.SeedPlant); // 수확 가능 알림음 (임시 재활용)
        }
        // ── 부패 시작: 최소 수확 윈도우 20초 보장 ────────────────────────────
        // requireRotTime * 0.5f 와 20s 중 큰 값 이후에 Rotting 진입
        else if (data.currentState == TileData.TileState.Harvestable
              && data.currentTimer >= Mathf.Max(20f, cropInfo.requireRotTime * 0.5f)
              && !data.isRotResistant)
        {
            data.currentState = TileData.TileState.Rotting;
            farm.UpdateTileVisual(pos, cropInfo.rottingTile);
            AudioManager.PlaySFX(SfxType.CropRot);
        }
        // ── 완전 고사 ────────────────────────────────────────────────────────
        else if (data.currentState == TileData.TileState.Rotting && data.currentTimer >= cropInfo.requireRotTime)
        {
            data.currentState = TileData.TileState.Dead;
            farm.UpdateTileVisual(pos, farm.deadTile);
            farm.activeCrops.Remove(pos);
        }
    }
}