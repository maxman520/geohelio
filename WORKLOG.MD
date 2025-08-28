chore(docs): AGENTS.md에 커밋 메시지용 WORKLOG 북마크 지침 추가

- 커밋 시 `WORKLOG.MD`에 북마크(`<!-- COMMIT_BOOKMARK -->`) 한 줄 기록
- 다음 커밋 메시지 작성 시 북마크 이후부터 문서 끝까지의 요약 활용
- 커밋 직후 북마크를 최신 위치로 갱신

chore(docs): AGENTS.md 에이전트 규칙 갱신 - WORKLOG 기록 조건 변경

- '커밋 메시지 작성 지시 시' 조건을 '단순 질문 답변을 제외한 나머지 코딩/파일 수정 작업 완료 시'로 수정
- 해당 규칙에 따라 본 변경 작업도 WORKLOG에 즉시 기록

chore(docs): AGENTS.md 승인(approve) 정책 추가

- 사용자 선호 반영: 파괴적 작업(파일/디렉터리 삭제, git 초기화/리셋 등) 외에는 승인 요청 없이 진행
- 예외 사항 명시: 샌드박스/네트워크 권한 필수 또는 데이터 손실 위험 시에만 사전 고지 후 승인 요청

chore(debug): PlayerRangeDebugger 삭제 및 관련 로그 정리

- 파일 삭제: Assets/Scripts/Debug/PlayerRangeDebugger.cs
- WORKLOG.MD에서 해당 컴포넌트 관련 기록 제거
  feat(player): PlayerController 공전 궤도 기즈모 표시 추가

- `distance` 값을 반지름으로 사용하여 현재 회전 중심(지구/태양) 기준 원 궤도 표시
- 탭으로 중심 전환 시 기즈모 중심도 실시간 변경
- 세그먼트 기반 단위 원 캐시로 GC 최소화, `orbitAxis`에 맞춰 평면 정합
  feat(core): GameManager 설계/구현 — 상태/점수/타이머/이벤트

- 파일 추가: Assets/Scripts/Game/GameManager.cs
- 상태머신(Init/Ready/Playing/Paused/GameOver), 점수/생명/타이머 관리, 이벤트 노출(OnStateChanged/OnScoreChanged/OnLivesChanged/OnGameStarted/OnGameOver)
- PlayerController 자동 참조(FindFirstObjectByType), 거리 조정 유틸 제공(SetPlayerDistance)

refactor(core): GameManager를 SingletonMonoBehaviour 상속으로 전환

- 내부 싱글톤 로직 제거(Instance/DontDestroyOnLoad 처리 제거)
- 베이스 클래스 초기화 호출로 일관된 싱글턴 보장
  feat(gameplay): GameScene 진행 로직 구성 (탭 시작/스폰/게임오버 UI)

- GameManager 확장: Ready 상태에서 첫 탭 입력으로 StartGame, GameOver 시 결과창 표시, 씬 전환 유틸(Main/Game/Restart)
- ObjectSpawner 추가(기존 AsteroidSpawner 개명): 간단 타이머 기반 스폰(Begin/Stop), 중심 기준 반경 스폰. 추후 장애물 스폰도 담당 예정
- GameOverPanel 추가: 점수 표시와 다시하기/메인으로 버튼 핸들러

refactor(ui): UIManager 추가 및 UI 로직 분리

- UIManager 생성: GameOverPanel 표시/숨김, 버튼 요청 처리(재시작/메인 이동)
- GameManager에서 GameOverPanel 직접 제어 제거 → UIManager 경유 호출로 변경
- GameOverPanel 버튼 핸들러가 UIManager를 통해 동작하도록 수정

refactor(ui): GameOverPanel TextMeshPro 전환 및 메인으로 버튼 제거

- 점수 표시 컴포넌트를 `Text` → `TMP_Text`로 교체, 한글 표기 정리
- 메인으로 버튼 및 관련 로직 제거(다시하기만 유지)

chore(git): GeneratedAssets 폴더 커밋 제외 처리

- 루트 `.gitignore`에 `/GeneratedAssets/`, `/GeneratedAssets_deleted/` 추가

chore(git): 생성 자산 폴더 정리 및 UI 스프라이트 추가 기록

- repo 정리: 루트 `GeneratedAssets/`, `GeneratedAssets_deleted/` 폴더 삭제
- .gitignore 반영 완료로 향후 커밋 제외 보장
- UI 스프라이트 추가: 버튼, 패널 배경, 아이콘 등 공용 리소스 추가(경로: `Assets/Sprites/` 하위)
  feat(core,player,ui): 게임 진행/스포너/UI 매니저 추가 및 궤도 기즈모, 구조 정리

- GameManager: 상태머신(Init/Ready/Playing/Paused/GameOver), Ready 상태 첫 탭으로 시작, 씬 전환 유틸(Main/Game/Restart), ObjectSpawner 연동, UIManager 경유 게임오버 표시
- ObjectSpawner: 타이머 기반 스폰 Begin/Stop, 중심 기준 반경 스폰. 기존 AsteroidSpawner를 ObjectSpawner로 개명
- UI: GameOverPanel 추가(점수 표시/다시하기/메인), UIManager 추가로 UI 제어 분리 및 버튼 핸들러 UIManager 경유 처리
- PlayerController: distance 기반 공전 궤도 기즈모 표시 추가, CurrentCenter/IsSunCenter 공개로 디버깅 용이성 향상
- refactor(core): GameManager를 SingletonMonoBehaviour 상속 구조로 전환(내부 싱글톤 로직 제거)
- docs(agents): AGENTS.md 규정 갱신(승인 정책 추가, WORKLOG 기록 조건 변경)
- chore(debug): PlayerRangeDebugger 시각화 컴포넌트 실험 후 요구 변경으로 제거
chore(git): .gitignore에서 AGENTS.md, WORKLOG.MD 제외

- `.gitignore`의 AGENTS.md, WORKLOG.MD 무시 규칙 제거
- 두 문서를 버전 관리에 포함하여 변경 이력 추적 가능
chore(docs): AGENTS.md에 커밋 메시지 요약 제외 규칙 추가

- 커밋 메시지 작성 시 `WORKLOG.MD` 요약을 참고하되,
- 생성 후 즉시 삭제되었거나 원상복구되어 실질 변화가 없는 작업은 메시지에서 제외하도록 지침 추가
