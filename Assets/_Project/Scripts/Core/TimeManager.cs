using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("시간 설정")]
    [Tooltip("기본 하루 길이(초). 80초 = 약 1분 20초")]
    public float realSecondsPerDay = 80f;

    [Tooltip("속도 배수. 1.0 = 기본, 1.5 = 1.5배 빠름, 2.0 = 2배 빠름. " +
             "런타임 중에도 즉시 반영됩니다.")]
    [Range(0.25f, 4f)]
    public float daySpeedMultiplier = 1f;

    public int startHour = 6;
    public int endHour = 24;

    private float elapsedTime = 0f;
    //  이전 시간을 기억해서 중복 실행을 막는 변수
    private int lastDisplayedHour = -1; 
    
    public int CurrentDay { get; private set; } = 1;

    public static event Action OnDayChanged;
    public static event Action<int, int> OnTimeChanged;
    public static event Action OnMidnight;
    public static event Action OnNightTension;
    // OnDebtSettlement 제거 — DailySettlementManager.CompleteSettlement()에서 직접 처리

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // daySpeedMultiplier가 클수록 시간이 빨리 흐름
        elapsedTime += Time.deltaTime * Mathf.Max(0.01f, daySpeedMultiplier);
        float dayProgress = elapsedTime / realSecondsPerDay;
        float totalHoursInDay = endHour - startHour;
        float currentTotalHours = startHour + (dayProgress * totalHoursInDay);
        
        int hour = Mathf.FloorToInt(currentTotalHours);
        int minute = Mathf.FloorToInt((currentTotalHours - hour) * 60);

        // UI 시간 업데이트
        OnTimeChanged?.Invoke(hour, minute);

        //  시간이 바뀌는 딱 그 순간에만 체크
        if (hour != lastDisplayedHour)
        {
            // 밤 11시가 되면 긴장 방송 송출!
            if (hour == 23)
            {
                OnNightTension?.Invoke();
                Debug.Log("🔔 째깍... 째깍... 자정까지 1시간 남았습니다!");
            }
            lastDisplayedHour = hour;
        }

        if (currentTotalHours >= endHour)
        {
            GoToNextDay();
        }
    }
    
    // SaveManager가 저장된 날짜를 복원할 때
    public void LoadDay(int day)
    {
        CurrentDay = day;
        Debug.Log($"[SaveManager] 날짜 복원: {CurrentDay}일차");
    }


    // event는 선언한 클래스 안에서만 Invoke 가능하므로 이 메서드로 우회
    [ContextMenu("⚡ 하루 강제 종료 (테스트)")]
    public void DEBUG_ForceNextDay() => GoToNextDay();

    public void DEBUG_ForceTimeSkip()
    {
        // 총 인게임 시간 계산 
        float totalHoursInDay = endHour - startHour;
        
        float secondsPerInGameHour = realSecondsPerDay / totalHoursInDay;
        
        elapsedTime += (secondsPerInGameHour * 4 );

    }

    private void GoToNextDay()
    {
        elapsedTime = 0f;
        lastDisplayedHour = -1;

        // ── 순서 중요 ────────────────────────────────────────────────────────
        // 1) OnMidnight만 발사 → DailySettlementManager가 timeScale=0 + UI 시작
        //    CurrentDay는 아직 이전 날 값 → DebtManager.GetTodayDebt()가 올바른 날짜 사용
        // 2) CurrentDay++, OnDayChanged, SaveAllData는
        //    DailySettlementManager.CompleteSettlement() 안의 AdvanceToNextDay()로 이동
        OnMidnight?.Invoke();
    }

    /// <summary>
    /// DailySettlementManager.CompleteSettlement()에서 호출.
    /// 정산 UI가 완전히 끝난 뒤 날짜를 올리고 OnDayChanged를 발사한다.
    /// </summary>
    public void AdvanceToNextDay()
    {
        CurrentDay++;
        OnDayChanged?.Invoke();
        Debug.Log($"[TimeManager] {CurrentDay}일차 아침이 밝았습니다.");
    }
}