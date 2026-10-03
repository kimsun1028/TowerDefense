## 맵 테마 (16:9 사이버 그리드)
- 길: 경로 중심선을 일정 폭으로 그려 매끈하게 (길 그림 = 적 경로, 길 판정 마스크)
- 2.5D: 길 두께(옆면)·그림자·윗면 음영, 픽셀아트(1px = 1텍셀, Point 필터)
- 입구: 블랙홀(Portal_Top / Portal_Right), 길이 구멍에서 나옴. SpawnPoint / GoalPoint 추가
- 타워 자리: 바닥의 작은 원 표시 (클릭 범위는 그대로)
- 맵 저장/불러오기: Tools > TD > Map > Saved Maps (Saved/Map01_Cyber 포함)
- 생성기: Tools/gen_map01.py (그림 + Map01Builder 좌표를 함께 생성)
## 확인
- develop(타워 코드 포함)과 컴파일 에러 0, 프리팹 Missing Script 0
- Map01 다시 만듦: 화면 16x9칸 (예전보다 약 7% 넓음, 타워 사거리 체감 확인 필요)
