2025-08-29 18:06 | feat | GameManager 설계/구현 — 상태·점수·타이머·이벤트 | Assets/Scripts/Game/GameManager.cs
2025-08-29 18:06 | refactor | GameManager를 SingletonMonoBehaviour 상속으로 전환 | Assets/Scripts/Game/GameManager.cs
2025-08-29 18:06 | feat | GameScene 진행 로직 구성 및 UI 연동 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/UI/UIManager.cs,Assets/Scenes/GameScene.unity
2025-08-29 18:06 | refactor | UIManager 추가 및 GameOverPanel 로직 분리 | Assets/Scripts/UI/UIManager.cs
2025-08-29 18:06 | refactor | GameOverPanel TMP 전환 및 버튼 정리 | Assets/Scripts/UI/\*
2025-08-29 18:06 | chore | GeneratedAssets 폴더 커밋 제외 및 정리 | .gitignore,GeneratedAssets,GeneratedAssets_deleted
2025-08-29 18:06 | refactor | 직렬화 private 필드 명명 규칙 통일(camelCase) | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Player/PlayerController.cs,Assets/Scripts/UI/UIManager.cs
2025-08-29 18:06 | fix | 한글 주석·문자열 복구 및 가독성 개선 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Player/PlayerController.cs,Assets/Scripts/UI/UIManager.cs
2025-08-29 18:06 | feat | 소행성 폭발/풀링 도입 및 디스폰 처리 | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:06 | refactor | SpawnedAsteroid 기능 Asteroid로 통합 후 제거 | Assets/Scripts/Game/SpawnedAsteroid.cs
2025-08-29 18:06 | fix | 태그 기반 충돌 판정으로 변경(OnTriggerEnter2D) | Assets/Scripts/Game/Asteroid.cs
2025-08-29 18:06 | fix | Asteroid_01.prefab에 Rigidbody2D 추가(트리거 미발동 수정) | Assets/Prefabs/Asteroid_01.prefab
2025-08-29 18:06 | feat | 스폰: 월드 원점 중심 + 화면 가로 절반 반지름 적용 | Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:06 | feat | Gizmo: 생성 가능 반경 시각화 추가 | Assets/Scripts/Game/ObjectSpawner.cs

2025-08-29 18:01 | docs | AGENTS.MD에 WORKLOG 작성 템플릿 규정 추가 | AGENTS.md,WORKLOG.MD
2025-08-29 18:03 | docs | WORKLOG를 북마크 바로 아래에 작성하도록 지침 추가 | AGENTS.md,WORKLOG.MD
2025-08-29 18:08 | docs | 과거 로그를 새 템플릿 기준으로 정리 완료 | WORKLOG.MD
2025-08-29 18:12 | feat | 스폰 시작 조건: Initialize 후 첫 중심 전환 이후로 지연(이전 탭 무시) | Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:16 | fix | 초기 배치 Initialize 시 생성, 주기 스폰은 첫 중심 전환 후 시작 | Assets/Scripts/Game/ObjectSpawner.cs

<!-- COMMIT_BOOKMARK -->

2025-08-30 02:53 | docs | 리팩터링 계획서 추가 | REFACTORING_PLAN.md
2025-09-01 13:28 | feat | PlayerController 중심 전환 이벤트 발행 추가(OnCenterToggled) | Assets/Scripts/Player/PlayerController.cs
2025-09-01 13:28 | refactor | ObjectSpawner 폴링 제거 및 PlayerController.OnCenterToggled 구독으로 시작 신호 처리 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-01 13:28 | feat | GameManager Ready 상태 첫 중심 전환 시 StartGame 옵션 추가(startOnFirstCenterToggle) | Assets/Scripts/Game/GameManager.cs
2025-09-01 13:45 | feat | 점수 UI 컨트롤러 추가 및 실시간 반영 | Assets/Scripts/UI/ScoreUIController.cs
2025-09-01 13:45 | revert | 하이스코어 저장/불러오기 로직 제거(추후 구현 예정) | Assets/Scripts/UI/GameOverPanel.cs,Assets/Scripts/Utilities/HighScoreStore.cs
2025-09-01 13:56 | refactor | ScoreUIController를 ScorePanel로 이름 변경 및 파일명 통일 | Assets/Scripts/UI/ScorePanel.cs
2025-09-01 14:02 | refactor | 점수 표시 천 단위 콤마 포맷 적용(예: 1,024) | Assets/Scripts/UI/ScorePanel.cs
2025-09-01 14:02 | feat | 소행성 파괴 시 10점 획득 로직 추가 | Assets/Scripts/Game/Asteroid.cs
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
2025-09-01 15:24 | refactor | SafeArea 직렬화/접근제어 정리 및 자동 참조 보완 | Assets/Scripts/UI/SafeArea.cs
2025-09-01 15:24 | chore | 공용 상수 클래스 추가(태그/애니메이터 트리거) 및 적용 | Assets/Scripts/Utilities/GameConstants.cs,Assets/Scripts/Game/Asteroid.cs
2025-09-01 15:26 | chore | .gitignore에 REFACTORING_PLAN.MD 추가 | .gitignore
