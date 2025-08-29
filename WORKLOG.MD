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

<!-- COMMIT_BOOKMARK -->

2025-08-29 18:01 | docs | AGENTS.MD에 WORKLOG 작성 템플릿 규정 추가 | AGENTS.md,WORKLOG.MD
2025-08-29 18:03 | docs | WORKLOG를 북마크 바로 아래에 작성하도록 지침 추가 | AGENTS.md,WORKLOG.MD
2025-08-29 18:08 | docs | 과거 로그를 새 템플릿 기준으로 정리 완료 | WORKLOG.MD
2025-08-29 18:12 | feat | 스폰 시작 조건: Initialize 후 첫 중심 전환 이후로 지연(이전 탭 무시) | Assets/Scripts/Game/ObjectSpawner.cs
2025-08-29 18:16 | fix | 초기 배치 Initialize 시 생성, 주기 스폰은 첫 중심 전환 후 시작 | Assets/Scripts/Game/ObjectSpawner.cs
