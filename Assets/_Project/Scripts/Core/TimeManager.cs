using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("시간 설정")] 
    public float realSecondsPerDay = 120f;
    public int startHour = 6;
    public int endHour = 24;
    
    private float elapsedTime = 0f;
    // ★ 추가: 이전 시간을 기억해서 중복 실행을 막는 변수
    private int lastDisplayedHour = -1; 
    
    public int CurrentDay { get; private set; } = 1;

    public static event Action OnDayChanged;
    public static event Action<int, int> OnTimeChanged;
    public static event Action OnMidnight;
    public static event Action OnNightTension;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
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
    
    private void GoToNextDay()
    {
        elapsedTime = 0f;
        CurrentDay++;
        // 다음 날을 위해 기억하던 시간을 초기화
        lastDisplayedHour = -1; 
        
        // 자정 정산 이벤트 먼저 발생
        OnMidnight?.Invoke();

        // 작물 성장을 위한 다음 날 알림 발생
        OnDayChanged?.Invoke(); 
        
        Debug.Log($"[TimeManager] {CurrentDay}일차 아침이 밝았습니다.");
    }
}