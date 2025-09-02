2025-08-29 18:01 | docs | AGENTS.MD에 WORKLOG 작성 템플릿 규정 추가 | AGENTS.md,WORKLOG.MD
2025-08-29 18:03 | docs | WORKLOG를 북마크 바로 아래에 작성하도록 지침 추가 | AGENTS.md,WORKLOG.MD
2025-08-29 18:06 | chore | GeneratedAssets 폴더 커밋 제외 및 정리 | .gitignore,GeneratedAssets,GeneratedAssets_deleted
2025-08-29 18:06 | feat | GameManager 설계/구현 — 상태·점수·타이머·이벤트 | Assets/Scripts/Game/GameManager.cs
2025-08-29 18:06 | feat | GameScene 진행 로직 구성 및 UI 연동 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/UI/UIManager.cs,Assets/Scenes/GameScene.unity
2025-08-29 18:06 | feat | Gizmo: 생성 가능 반경 시각화 추가 | Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:06 | feat | 소행성 폭발/풀링 도입 및 디스폰 처리 | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:06 | feat | 스폰: 월드 원점 중심 + 화면 가로 절반 반지름 적용 | Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:06 | fix | Asteroid_01.prefab에 Rigidbody2D 추가(트리거 미발동 수정) | Assets/Prefabs/Asteroid_01.prefab
2025-08-29 18:06 | fix | 태그 기반 충돌 판정으로 변경(OnTriggerEnter2D) | Assets/Scripts/Game/Asteroid.cs
2025-08-29 18:06 | fix | 한글 주석·문자열 복구 및 가독성 개선 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Player/PlayerController.cs,Assets/Scripts/UI/UIManager.cs
2025-08-29 18:06 | refactor | GameManager를 SingletonMonoBehaviour 상속으로 전환 | Assets/Scripts/Game/GameManager.cs
2025-08-29 18:06 | refactor | GameOverPanel TMP 전환 및 버튼 정리 | Assets/Scripts/UI/\*
2025-08-29 18:06 | refactor | SpawnedAsteroid 기능 Asteroid로 통합 후 제거 | Assets/Scripts/Game/SpawnedAsteroid.cs
2025-08-29 18:06 | refactor | UIManager 추가 및 GameOverPanel 로직 분리 | Assets/Scripts/UI/UIManager.cs
2025-08-29 18:06 | refactor | 직렬화 private 필드 명명 규칙 통일(camelCase) | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Player/PlayerController.cs,Assets/Scripts/UI/UIManager.cs
2025-08-29 18:08 | docs | 과거 로그를 새 템플릿 기준으로 정리 완료 | WORKLOG.MD
2025-08-29 18:12 | feat | 스폰 시작 조건: Initialize 후 첫 중심 전환 이후로 지연(이전 탭 무시) | Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:16 | fix | 초기 배치 Initialize 시 생성, 주기 스폰은 첫 중심 전환 후 시작 | Assets/Scripts/Game/ObjectSpawner.cs
2025-08-30 02:53 | docs | 리팩터링 계획서 추가 | REFACTORING_PLAN.md
2025-09-01 13:28 | feat | GameManager Ready 상태 첫 중심 전환 시 StartGame 옵션 추가(startOnFirstCenterToggle) | Assets/Scripts/Game/GameManager.cs
2025-09-01 13:28 | feat | PlayerController 중심 전환 이벤트 발행 추가(OnCenterToggled) | Assets/Scripts/Player/PlayerController.cs
2025-09-01 13:28 | refactor | ObjectSpawner 폴링 제거 및 PlayerController.OnCenterToggled 구독으로 시작 신호 처리 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 13:45 | feat | 점수 UI 컨트롤러 추가 및 실시간 반영 | Assets/Scripts/UI/ScoreUIController.cs
2025-09-01 13:45 | revert | 하이스코어 저장/불러오기 로직 제거(추후 구현 예정) | Assets/Scripts/UI/GameOverPanel.cs,Assets/Scripts/Utilities/HighScoreStore.cs
2025-09-01 13:56 | refactor | ScoreUIController를 ScorePanel로 이름 변경 및 파일명 통일 | Assets/Scripts/UI/ScorePanel.cs
2025-09-01 14:02 | feat | 소행성 파괴 시 10점 획득 로직 추가 | Assets/Scripts/Game/Asteroid.cs
2025-09-01 14:02 | refactor | 점수 표시 천 단위 콤마 포맷 적용(예: 1,024) | Assets/Scripts/UI/ScorePanel.cs
2025-09-01 14:06 | fix | GameManager.OnDestroy() 경고 수정 — base 호출 및 override 적용 | Assets/Scripts/Game/GameManager.cs
2025-09-01 14:20 | feat | 공전 중심 콤보 점수 도입(10→20→30…; 중심 변경 시 초기화) | Assets/Scripts/Game/GameManager.cs
2025-09-01 14:20 | refactor | 소행성 점수 지급 타이밍을 애니메이션 트리거 직후로 변경 | Assets/Scripts/Game/Asteroid.cs
2025-09-01 14:28 | feat | 소행성 스폰 시 알파 페이드 인(0→1) 이펙트 추가 | Assets/Scripts/Game/Asteroid.cs
2025-09-01 14:36 | feat | 소행성 스폰 후 무작위 회전(방향/속도) 및 폭발 시 회전 정지 | Assets/Scripts/Game/Asteroid.cs
2025-09-01 14:40 | fix | Random 충돌 해결 — UnityEngine.Random 별칭 사용 | Assets/Scripts/Game/Asteroid.cs
2025-09-01 14:45 | refactor | 2D 전용 회전 보정(Transform X/Y=0 유지) 및 Rigidbody2D 연동(angularVelocity) | Assets/Scripts/Game/Asteroid.cs,Assets/Prefabs/Asteroid_01.prefab
2025-09-01 14:52 | refactor | 소행성 회전 기능 제거, 드리프트(부유감)만 유지 | Assets/Scripts/Game/Asteroid.cs
2025-09-01 15:00 | feat | 화면 경계(카메라 반경+마진) 이탈 시 소행성 자동 디스폰 | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/Asteroid.cs
2025-09-01 15:06 | feat | 디스폰 경계 기즈모 표시(카메라 중심 원 + 마진) 추가 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 15:14 | feat | 회전 중심이 스폰 영역(원점 중심 원)+마진을 벗어나면 게임오버 처리 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 15:18 | fix | 게임오버 시 플레이어 클릭(탭) 입력 무시 처리 | Assets/Scripts/Player/PlayerController.cs
2025-09-01 15:24 | chore | 공용 상수 클래스 추가(태그/애니메이터 트리거) 및 적용 | Assets/Scripts/Utilities/GameConstants.cs,Assets/Scripts/Game/Asteroid.cs
2025-09-01 15:24 | refactor | SafeArea 직렬화/접근제어 정리 및 자동 참조 보완 | Assets/Scripts/UI/SafeArea.cs
2025-09-01 15:26 | chore | .gitignore에 REFACTORING_PLAN.MD 추가 | .gitignore
2025-09-01 15:30 | refactor | 스포너 AddComponent 제거, 프리팹 구성 검증(OnValidate) 추가 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 15:48 | refactor | 게임오버 임계 반경에서 centerOutMargin 제거(스폰 반경 그대로 사용) | Assets/Scripts/Game/GameManager.cs
2025-09-01 15:55 | docs | 점수 팝업 설계(F): ObjectSpawner 통합안으로 재작성 | REFACTORING_PLAN.md
2025-09-01 16:00 | docs | REFACTORING_PLAN.md를 F 단계(점수 팝업 통합안)만 남기고 나머지 삭제 | REFACTORING_PLAN.md
2025-09-01 16:06 | feat | 점수 팝업(플로팅 텍스트) — ObjectSpawner 통합(풀/애니메이션/표시) | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/GameManager.cs
2025-09-01 16:12 | fix | UniTaskVoid 메서드 내 'yield break' 사용 오류 수정(return으로 조기 종료) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 16:16 | refactor | 점수 팝업 위치 지터 제거(정확한 폭발 지점에 표시) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 18:05 | feat | 플레이어 플래시 연출용 화이트 오버레이 셰이더/머티리얼 추가 | Assets/Shaders & Materials/WhiteOverlay.shader,Assets/Shaders & Materials/PlayerWhiteFlash.mat
2025-09-01 18:13 | feat | 플레이어 Idle/Hurt 애니메이션 및 컨트롤러 추가 | Assets/Animation/\_Character/Idle.anim,Assets/Animation/\_Character/Hurt.anim,Assets/Animation/\_Character/Player.controller
2025-09-01 18:20 | feat | 장애물 소행성 스폰 추가(단계별 최대치 2/3/4, 30초 간격, 최소 간격 3) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 18:28 | feat | 장애물 충돌 시 플레이어 Hurt 애니메이션 재생(Animator.Play, 상수화) | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Utilities/GameConstants.cs
2025-09-01 18:28 | feat | 장애물-일반 소행성 간 최소 간격 적용 및 장애물 플래그/점수 미지급 반영 | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/Asteroid.cs
2025-09-01 18:35 | refactor | 하드코딩 문자열을 GameConstants로 치환(씬명/애니메이터 트리거 기본값) | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/UI/MainMenuController.cs,Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Utilities/GameConstants.cs

2025-09-02 16:47 | refactor | PlayerController의 FormerlySerializedAs 특성 제거 및 불필요 using 정리 | Assets/Scripts/Player/PlayerController.cs
2025-09-02 17:09 | feat | 슈팅스타 엔티티 추가(포물선 이동, 플레이어 Hurt, 방향 보정) | Assets/Scripts/Game/ShootingStar.cs
2025-09-02 17:09 | feat | 스포너에 슈팅스타 스폰/풀링/디스폰/검증 및 타이머 추가 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-02 17:30 | fix | 일반 풀에 슈팅스타가 섞이는 레이스 조건 수정(스폰 순서/폴백 처리 보완) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-02 17:46 | refactor | ObjectSpawner를 유니티 내장 풀(Dictionary<string,ObjectPool>)로 리팩터링 — 기존 기능 유지 | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Utilities/PooledObjectTag.cs
2025-09-02 17:51 | fix | 점수 팝업 토큰 변수명 섀도잉 컴파일 오류(CS0136) 수정 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-02 18:03 | refactor | 점수 플로팅 텍스트 로직을 ScoreFloatingText 컴포넌트로 이관, 스포너 변수명/로직 정리 | Assets/Scripts/UI/ScoreFloatingText.cs,Assets/Scripts/Game/ObjectSpawner.cs
2025-09-02 18:08 | fix | 플로팅 텍스트 파괴 시 예외 방지(취소 토큰/방어 코드 추가) | Assets/Scripts/UI/ScoreFloatingText.cs
2025-09-02 18:10 | fix | ScoreFloatingText에 OnDisable/OnDestroy 취소 연동 추가(풀 반환 즉시 중지) | Assets/Scripts/UI/ScoreFloatingText.cs
2025-09-02 18:12 | fix | GetCancellationTokenOnDisable 미지원 환경 대응 — 파괴 토큰만 사용하고 OnDisable에서 수동 취소 | Assets/Scripts/UI/ScoreFloatingText.cs
2025-09-02 18:16 | fix | 슈팅스타 즉시 디스폰 현상 수정 — 화면 내부 최초 진입 이후에만 이탈 시 디스폰 | Assets/Scripts/Game/ShootingStar.cs
2025-09-02 18:16 | refactor | ScoreFloatingText에서 파괴 토큰 링크 제거, 단순 CTS로 정리 | Assets/Scripts/UI/ScoreFloatingText.cs
2025-09-02 18:27 | refactor | 슈팅스타 이동 로직 속도 기반으로 전환(베지어 아크 길이 근사), 스폰 스케줄 규칙(초기 지연/최대치 쿨타임 후 랜덤) 반영 | Assets/Scripts/Game/ShootingStar.cs,Assets/Scripts/Game/ObjectSpawner.cs
2025-09-02 18:29 | refactor | 내장 풀 전환 후 미사용 코드/필드 정리(ObjectSpawner) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-02 18:30 | fix | 참조 제거 누락으로 인한 컴파일 오류(shootingStarDurationRange) 제거 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-02 18:43 | docs | WORKLOG 시간순 정렬 및 최신 로그 위치 규칙 변경 | WORKLOG.MD,AGENTS.MD
2025-09-02 19:27 | fix | 슈팅스타 이탈 기준을 카메라 사각형+마진으로 분리 적용(ObjectSpawner 확장) | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-02 19:28 | feat | 기즈모 보강 — 슈팅스타용 사각형 디스폰 경계(카메라 뷰+마진) 시각화 추가 | Assets/Scripts/Game/ObjectSpawner.cs
<!-- COMMIT_BOOKMARK -->
