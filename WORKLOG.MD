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

refactor(serialization,naming): 직렬화 private 필드 명명 규칙 통일(camelCase)

- 규칙 반영: `[SerializeField] private camelCase`로 일괄 변경, 비직렬화 private 필드는 `_camelCase` 유지
- 안전 마이그레이션: 일부 스크립트에 `FormerlySerializedAs` 추가해 씬/프리팹 참조 보존
- 변경 파일: `Assets/Scripts/Game/Asteroid.cs`, `Assets/Scripts/Game/GameManager.cs`, `Assets/Scripts/Game/ObjectSpawner.cs`, `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/UI/UIManager.cs`
- 참고: 코드 내 모든 참조도 함께 갱신하여 컴파일 오류가 없도록 정리
fix(i18n,comments): GameManager/PlayerController/UIManager 한글 주석·문자열 복구

- 손상된 한글 주석/문자열(모지바케) 복구 및 가독성 개선
- `GameManager.cs`: 요약 주석/툴팁/로그 한국어 정비, 씬/스포너 안내 문구 정리
- `PlayerController.cs`: 요약/헤더/툴팁/경고/로그/디버그 주석을 자연스러운 한국어로 재작성
- `UIManager.cs`: 헤더/경고 로그 한국어 수정 및 간결화
- 직렬화 안전 위해 필요한 곳에 `FormerlySerializedAs` 보강
fix(i18n,comments): ObjectSpawner 한글 주석·문자열 복구

- 파일 재작성 수준으로 모지바케 복구 및 주석 가독성 개선
- 스폰 규칙/툴팁/경고/로그 문자열을 자연스러운 한국어로 정리
- 기존 로직과 필드명은 유지(camelCase), 기능 변화 없음
refactor(asteroid): 충돌 대신 트리거 사용 및 툴팁 추가

- OnCollisionEnter2D 제거, OnTriggerEnter2D만 사용하도록 정리
- `fallbackExplodeDuration`에 한국어 툴팁 추가(애니메이터 태그 부재 시 대체 대기 시간)
- 파일: `Assets/Scripts/Game/Asteroid.cs`
chore(docs): 커밋 완료 시 북마크 즉시 기록 지침 추가 및 북마크 최신화

- AGENTS.md에 '커밋 완료 즉시 WORKLOG 끝에 북마크 남기기' 규칙 추가
- 본 작업 후 문서 끝에 커밋 북마크(`<!-- COMMIT_BOOKMARK -->`)를 최신 위치로 갱신
chore(git): post-commit 훅 추가 — 커밋 직후 WORKLOG 북마크 자동 최신화

- 파일 추가: `.git/hooks/post-commit`
- 동작: 기존 북마크 라인을 제거하고 문서 끝에 `<!-- COMMIT_BOOKMARK -->` 한 줄 추가
- 비고: 후속 커밋에 포함되지는 않으며 워킹 트리에만 반영
chore(docs): AGENTS.md 북마크 지침 간결화

- 북마크 수동 추가/갱신 지침 제거 (post-commit 훅 자동화 전제)
- 커밋 메시지 작성 시 북마크 이후~끝 요약 활용 규칙만 유지
feat(spawner,core): ObjectSpawner 초기화/규칙 구현 및 GameManager 연동

- 요구사항 반영: 탭으로 시작 로직 제거, Initialize 시 기존 소행성 전부 삭제 후 초기 개수 생성 및 즉시 스폰 시작
- 규칙 추가: 일반 스폰 시 플레이어 공전 원 내부 금지(Initialize 초기 생성에는 미적용), 반경 0.5 유닛 이내 중복 배치 금지
- 관리 방식 개선: 스폰된 소행성 트래킹(List) 및 파괴 통지용 `SpawnedAsteroid` 컴포넌트 추가로 일관된 정리 처리
- GameManager 연동: `ToReady()`에서 `ObjectSpawner.Initialize()` 호출로 씬 진입/Retry 시 초기화 및 스폰 시작, `StartGame()`의 스폰 시작 호출 제거
- 파일 추가: `Assets/Scripts/Game/SpawnedAsteroid.cs`
- 파일 수정: `Assets/Scripts/Game/ObjectSpawner.cs`, `Assets/Scripts/Game/GameManager.cs`
refactor(style): 명명 규칙 지침 추가 및 코드 통일(_camelCase)

- AGENTS.md 갱신: 지역/매개변수 `camelCase`, 클래스·메서드·속성·public 멤버 `PascalCase`, private 필드 `_camelCase` 규칙 명시 강화
- 코드 정리: 직렬화 private 필드를 `_camelCase`로 통일하고 `FormerlySerializedAs`로 인스펙터 값 보존
- 변경 파일: `GameManager.cs`, `ObjectSpawner.cs`, `PlayerController.cs`, `UIManager.cs`
feat(gameplay,pool): 소행성 폭발/풀링 도입

- 소행성-플레이어 충돌 시 애니메이션 트리거("explode") 재생 후 종료까지 대기, 이후 제거 대신 스포너 풀로 반환
- `Asteroid` 컴포넌트 추가(충돌/폭발/풀 반환 관리, UniTask 사용)
- `ObjectSpawner`에 오브젝트 풀(Queue) 적용: 생성 시 풀 재사용, 초기화/정리 시 파괴 대신 비활성화 후 풀로 반환
- 초기 생성 로직/정리 로직을 풀 기반 동작으로 변경하여 생성/제거 비용 절감
- 파일 추가: `Assets/Scripts/Game/Asteroid.cs`
refactor(pool): SpawnedAsteroid 기능 Asteroid로 통합 및 제거

- 파괴 통지 마커(`SpawnedAsteroid`) 역할을 `Asteroid` 컴포넌트 내에서 디스폰 호출로 대체
- 파일 삭제: `Assets/Scripts/Game/SpawnedAsteroid.cs`
chore(docs): AGENTS.md 지침에 command approval 생략 규칙 명시

- 파괴적 작업(파일/디렉터리 삭제 등) 제외, 파일 읽기/폴더 탐색 등 비파괴 작업은 승인 없이 즉시 실행
- 코딩 작업(스크립트 추가·수정)은 승인 없이 즉시 실행
- 위 지침을 AGENTS.md의 Agent-Specific Instructions 섹션에 추가


fix(asteroid): OnTriggerEnter2D 태그 "Player" 비교로 충돌 판정

- 기존 PlayerController 컴포넌트 검사 방식을 제거하고 `CompareTag("Player")`로 단순화
- 플레이어 하위 콜라이더 대응을 위해 `other.transform.root.CompareTag("Player")`도 함께 확인

fix(physics): Asteroid_01.prefab에 Rigidbody2D(Kinematic) 추가 — 트리거 미발동 수정

- 원인: 충돌쌍에 Rigidbody2D 부재로 `OnTriggerEnter2D`가 호출되지 않음
- 변경: Asteroid 프리팹 루트에 `Rigidbody2D` 추가(BodyType: Kinematic, GravityScale: 0, Simulated: On)
- 효과: 플레이어 자식 콜라이더와 겹칠 때 소행성 트리거 정상 호출 → 폭발/디스폰 로직 동작

<!-- COMMIT_BOOKMARK -->
feat(spawner): 월드 원점 중심 + 화면 가로 절반 반지름 스폰으로 변경

- 스폰 중심을 고정적으로 월드 원점(0,0)으로 변경(기존 `center` 참조 무시)
- 스폰 반경을 직교 카메라 기준 화면 가로 길이의 절반으로 계산(`orthographicSize * aspect`), 카메라 미탐색 시 기존 `spawnRadius` 폴백
- 초기 스폰도 플레이어 공전 범위(현재 중심 기준 원) 내부를 제외하도록 규칙 적용(기존: 초기만 예외)
- 원의 둘레가 아닌 원 내부 균등 분포로 스폰(`Random.insideUnitCircle` 사용; 기존 `normalized` 제거)
- 최소 간격(0.5) 체크 로직은 그대로 유지
- 파일: `Assets/Scripts/Game/ObjectSpawner.cs`

feat(spawner,debug): 소행성 생성 가능 범위 Gizmo 시각화

- Scene 뷰에서 외곽 생성 반경(월드 원점, 화면 가로 절반) 와이어 원 표시
- 플레이어 공전 반경(생성 금지 영역) 와이어 원과 중심점 마커 표시
- 파일: `Assets/Scripts/Game/ObjectSpawner.cs` (OnDrawGizmosSelected 추가)

fix(spawner,gizmo): 내부 금지 구역 표시 제거 및 외곽 반경 시각화 보정

- 요구사항 반영: 내부 금지 구역(플레이어 공전 원) 기즈모 표시 제거
- 외곽 생성 반경 계산을 런타임 로직과 일치하도록 보정
  - 플레이 중: `GetSpawnRadiusWorld()` 그대로 사용
  - 에디터: 직교 `Camera.main` 우선, 없으면 모든 카메라 중 직교 카메라 탐색, 실패 시 `spawnRadius` 폴백
- `Gizmos.matrix`를 `identity`로 강제하여 외부 행렬 변경 영향 제거
- 파일: `Assets/Scripts/Game/ObjectSpawner.cs` (OnDrawGizmosSelected, GetGizmoSpawnRadius 추가)

fix(spawner,gizmo): 선택 여부와 무관하게 영역 표시되도록 변경

- `OnDrawGizmosSelected` → `OnDrawGizmos`로 교체하여 Hierarchy에서 미선택 상태에서도 생성 가능 영역이 보이도록 수정
- 내부 금지 구역 미표시, 외곽 반경 계산 및 행렬 초기화 로직은 유지
- 파일: `Assets/Scripts/Game/ObjectSpawner.cs`

feat(spawner): 스폰 시 소행성 Z 회전 랜덤화

- 새로 생성되거나 풀에서 꺼낸 소행성을 스폰할 때 Z 축 회전을 0~360도 범위에서 랜덤 지정
- 연출 다양화 및 화면 지루함 감소 목적
- 파일: `Assets/Scripts/Game/ObjectSpawner.cs` (SpawnAt에서 `Quaternion.Euler(0,0,Random)` 적용)
