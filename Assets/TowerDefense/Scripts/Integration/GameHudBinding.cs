// AI-assisted code start
using UnityEngine;

/// <summary>
/// 타워 팀 GameManager ↔ 우리 HUD(GameHud) 연결 (타워↔UI 약속의 "게임 상태").
///   GoldChanged / LivesChanged → HUD 골드·목숨 표시
///   GameWon / GameLost → 승리/패배 창
/// 골드 계산은 GameManager가 하고, 여기서는 표시만 한다.
/// GameManager는 Start에서 처음 값을 알리므로, Start 전에(Awake 다음 프레임 전) 구독하고 지금 값도 바로 넣어준다.
/// UI 어셈블리(TD.UI)는 GameManager를 참조할 수 없어서 이 연결은 asmdef 없는 폴더에 둔다.
/// </summary>
public class GameHudBinding : MonoBehaviour {
    // 2. 필드
    private GameManager gameManager;
    private GameHud hud;

    // 4. 메서드 (private)
    private void Start() {
        TryBind();
    }

    private void Update() {
        if (gameManager == null || hud == null) {
            TryBind(); // 둘 중 하나가 늦게 생긴 경우
        }
    }

    private void OnDestroy() {
        Unbind();
    }

    private void TryBind() {
        if (gameManager != null && hud != null) {
            return;
        }
        GameManager foundManager = GameManager.Instance;
        GameHud foundHud = GameHud.Instance;
        if (foundManager == null || foundHud == null) {
            return;
        }
        Unbind();
        gameManager = foundManager;
        hud = foundHud;
        gameManager.GoldChanged += hud.SetGold;
        gameManager.LivesChanged += hud.SetLives;
        gameManager.GameWon += ShowWin;
        gameManager.GameLost += ShowLose;
        hud.SetGold(gameManager.Gold);
        hud.SetLives(gameManager.Lives);
    }

    private void Unbind() {
        if (gameManager != null && hud != null) {
            gameManager.GoldChanged -= hud.SetGold;
            gameManager.LivesChanged -= hud.SetLives;
            gameManager.GameWon -= ShowWin;
            gameManager.GameLost -= ShowLose;
        }
        gameManager = null;
        hud = null;
    }

    private void ShowWin() {
        hud.ShowResult(true);
    }

    private void ShowLose() {
        hud.ShowResult(false);
    }
}
// AI-assisted code end
