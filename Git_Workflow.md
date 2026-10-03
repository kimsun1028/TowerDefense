## Branch

- `main` : 최종/안정 버전
- `develop` : 개발 통합(default)
- `feature/*` : 새 기능 개발
- `fix/*` : 버그 수정
- `change/*` : 요구사항 변경 수정

```
작업 흐름

develop 최신화
feature/fix/change 브랜치 생성
개발
commit
push
PR
develop merge
브랜치 삭제
다음 작업
```

## Commit

- `feat` : 새로운 기능
- `fix` : 버그 수정
- `refactor` : 구조 개선
- `chore` : 기타 작업

```
예시

feat: implement spawn system
fix: prevent piece moving through wall
refactor: separate score logic
```