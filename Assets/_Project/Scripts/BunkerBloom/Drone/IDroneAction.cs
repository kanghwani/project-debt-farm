using System;
using System.Collections.Generic;

// 책임: 드론 명령의 공통 계약 (OCP)
// 새 명령(WaterAction, ApplyDrugAction 등)은 이 인터페이스 구현만으로 추가 가능 — 코어 코드 수정 X
public interface IDroneAction
{
    /// <summary>이 명령이 소모하는 자원 (Power, Water 등)</summary>
    Dictionary<ResourceType, float> ResourceCost { get; }

    /// <summary>대상 plot 상태 + 자원 잔량을 보고 실행 가능 여부 판단</summary>
    bool CanExecute(CropPlot plot);

    /// <summary>실행. 비동기(타이밍 바 등)일 수 있으므로 onComplete 콜백 제공.</summary>
    void Execute(CropPlot plot, Action onComplete = null);
}
