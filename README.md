## 게임 UI (맵 테마와 같은 픽셀아트)
- HUD: 목숨·골드, WAVE n/15, 일시정지·배속 (GameHud.SetLives / SetGold / SpeedRequested)
- 웨이브: 쉬는 시간(15초)에만 입구 옆 블랙홀 버튼(누르면 바로 시작, 고리 = 남은 시간) + 길 위 예상 경로 화살표
- 웨이브 끝 = 필드의 마지막 적이 죽거나 지키는 곳에 들어갔을 때 (WaveSpawner)
- 타워 링 메뉴: RingMenu.Instance.Open(위치, List<RingMenuOption>) — 타워 코드에 의존하지 않음
- 정보 바: 한 줄(초상화·이름 탭·아이콘+값)
- 폰트: 나눔고딕(OFL) + 어두운 외곽선, TMP 기본 리소스 포함

## 붙이는 법
- 씬에 Prefabs/UI/GameHud, InfoPanel + EventSystem(Input System UI Input Module)
- 게임 매니저: GameHud.Instance.SetLives/SetGold, SpeedRequested 구독
- 타워 쪽: 자리 클릭 시 RingMenuOption 목록 만들어 RingMenu.Instance.Open 호출

## 확인
- 맵 테마 브랜치 + develop과 컴파일 에러 0, 프리팹 Missing Script 0
- ⚠ feature/map-theme(SpawnPoint) 위에 쌓은 브랜치 → 맵 테마 PR 먼저 머지
