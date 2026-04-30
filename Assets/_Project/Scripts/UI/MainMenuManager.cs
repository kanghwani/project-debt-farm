using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI 연결")]
    [Tooltip("Hierarchy 창의 Continue 버튼을 드래그해서 넣으세요.")]
    public Button continueButton;

    private void Start()
    {
        // 안전장치
        if (continueButton == null)
        {
            Debug.LogError("이어하기(Continue) 버튼이 인스펙터에 연결되지 않았습니다!");
            return;
        }

        // 저장된 Day 데이터가 있는지 확인해서 계속하기 버튼 상태 결정
        if (PlayerPrefs.HasKey(SaveManager.KEY_DAY))
        {
            continueButton.interactable = true;
        }
        else
        {
            // 저장된 게 없으면 버튼을 회색으로 꺼버림 (클릭 방지)
            continueButton.interactable = false;
        }
    }

    public void OnClickNewGame()
    {
        // 올 클리어
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 새 게임은 인트로 씬을 먼저 거친다
        SceneManager.LoadScene("IntroScene");
    }

    public void OnClickContinue()
    {
        // 데이터 로드는 씬이 켜질 때 각 매니저들이 알아서 하니까 여기선 씬만 넘김
        SceneManager.LoadScene("GameScene");
    }

    public void OnClickQuit()
    {
        // 에디터에서는 실제로 꺼지지 않으므로, 잘 작동하는지 확인하기 위한 로그
        Debug.Log("게임을 종료합니다."); 
        
        // 게임 종료 (에디터에선 반응 없고 빌드된 게임에서만 꺼짐)
        Application.Quit();
    }
}