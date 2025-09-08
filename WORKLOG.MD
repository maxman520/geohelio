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

2025-09-03 01:10 | feat | 태양→지구 빔 정렬/수명 보정 스크립트 추가(SunBeamVfx) | Assets/Scripts/Player/SunBeamVfx.cs
2025-09-03 01:40 | refactor | SunBeamVfx를 Local 시뮬레이션 공간 기반으로 동작하도록 옵션/경고 수정 | Assets/Scripts/Player/SunBeamVfx.cs
2025-09-03 01:43 | refactor | SunBeamVfx에 정렬 축 선택/반전 옵션 추가(Up/Right/Forward) | Assets/Scripts/Player/SunBeamVfx.cs
2025-09-03 01:47 | refactor | SunBeamVfx 정렬 로직을 Forward 고정으로 단순화(옵션 제거) | Assets/Scripts/Player/SunBeamVfx.cs
2025-09-03 01:59 | refactor | SunBeamVfx의 자동 수명 보정 로직 제거(파티클 시스템에서 제어) | Assets/Scripts/Player/SunBeamVfx.cs
2025-09-03 02:00 | refactor | SunBeamVfx에서 useLocalSimulationSpace/alignForwardToEarth 옵션 제거 및 Local+Forward 고정 | Assets/Scripts/Player/SunBeamVfx.cs

2025-09-03 15:52 | feat | 슈팅스타 예측 경로(LineRenderer) 구현 — 전체 경로 고정+알파로 앞부분 지우기, 타일링 1회 설정 | Assets/Scripts/Game/ShootingStar.cs
2025-09-03 16:10 | feat | 슈팅스타 스폰/디스폰 로직 수정 — 시작/도착 지점: 카메라 뷰~마진 띠 영역, 디스폰: u>=1로 통일 | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-03 16:16 | fix | 슈팅스타 경유 지점 정확 통과(viaProgress 기반 핸들 역산) | Assets/Scripts/Game/ShootingStar.cs
2025-09-03 16:16 | refactor | trajectoryMaterial 및 라인 소팅 관련 변수 제거(정렬은 LineRenderer에서 직접 설정) | Assets/Scripts/Game/ShootingStar.cs
2025-09-03 16:22 | fix | 슈팅스타 도착 지점 로직 개선 — 경유 지점 방향으로 뷰 경계~마진 경계 사이에서 샘플 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-03 16:25 | fix | 'dir' 변수 중복 선언 오류 해결 — 로컬명 충돌 회피(toViaDir) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-03 16:36 | refactor | 슈팅스타 경로를 직선으로 단순화(Bezier 제거), 출발/경유/도착 동일 로직(밴드 샘플) 적용 | Assets/Scripts/Game/ShootingStar.cs,Assets/Scripts/Game/ObjectSpawner.cs
2025-09-03 16:52 | refactor | 슈팅스타 경로 로직 변경 — 도착 지점: (0,0) 기준 거울, 이동: 간단한 포물선 오프셋(4u(1-u)) 적용, 경유 지점 로직/변수 제거 | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-03 17:16 | feat | 슈팅스타 경로가 플레이어를 관통하도록 조정 — 가로 경로는 y=플레이어y+랜덤오프셋, 세로 경로는 x=플레이어x+랜덤오프셋으로 정렬. 포물선 오프셋 방향도 축 기준으로 고정 | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-03 17:35 | fix | 슈팅스타 경로가 직선이 되는 문제 수정 — 2차 베지어로 변경하고, u=0.5에서 플레이어 축(+오프셋) 통과 지점을 보장하도록 제어점 산출. 스포너는 passPoint만 계산해 전달 | Assets/Scripts/Game/ObjectSpawner.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-03 17:35 | fix | 다시하기 후 중심 전환 시 콤보가 초기화되지 않는 버그 수정 — StartOnFirstCenterToggle 옵션과 무관하게 중심 전환마다 콤보를 0으로 리셋 | Assets/Scripts/Game/GameManager.cs
2025-09-03 17:39 | fix | 씬 재시작 시 싱글톤 인스턴스의 구독/초기화 누락 보완 — sceneLoaded에서 Player/Spawner 재바인딩 및 ToReady 재호출로 콤보/점수/상태 초기화 보장 | Assets/Scripts/Game/GameManager.cs

2025-09-04 01:22 | feat | 블랙홀 스프라이트 및 프리팹 추가 | Assets/Prefabs/Blackhole.prefab,Assets/Sprites/\_Objects/Blackhole_Idle.aseprite,Assets/Sprites/\_Objects/Blackhole_Idle1.png
2025-09-04 01:40 | feat | 블랙홀 프리팹에 흡입 파티클 시스템 추가 | Assets/Prefabs/Blackhole.prefab

2025-09-04 14:21 | feat | 블랙홀 스크립트/스포너 추가(단일 존재 보장, 수명 5s, 20~30s 랜덤 지연, 공전 범위 회피 스폰, 기즈모 시각화) | Assets/Scripts/Game/BlackHole.cs,Assets/Scripts/Game/BlackHoleSpawner.cs
2025-09-04 14:28 | refactor | BlackHoleSpawner 기능 ObjectSpawner에 통합(루프/스폰 규칙/기즈모), 단일 블랙홀 수명/간격 유지 및 GameOver 연동 | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-04 14:28 | chore | 사용 중단된 BlackHoleSpawner 삭제 | Assets/Scripts/Game/BlackHoleSpawner.cs
2025-09-04 14:38 | refactor | ObjectSpawner 코드 영역을 #region으로 구분(일반 공통/소행성/장애물/슈팅스타/블랙홀) 및 블랙홀 Gizmo 항상 On, 색상 Blue 고정(인스펙터 옵션 제거) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-04 14:43 | fix | ObjectSpawner의 #endregion 중복으로 인한 전처리기 오류 해결(불필요한 종료 지시문 제거) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-04 14:56 | feat | 블랙홀이 플레이어를 끌어당기는 힘 추가(세기/범위 직렬화 가능) | Assets/Scripts/Game/BlackHole.cs
2025-09-04 15:02 | refactor | 블랙홀 흡인 방식을 물리 힘(AddForce)에서 공전 중심 이동으로 변경 | Assets/Scripts/Game/BlackHole.cs

2025-09-04 15:17 | fix | 다시하기 후 게임오버 패널이 표시되지 않는 문제 수정(비활성 오브젝트 포함 탐색으로 참조 재획득) | Assets/Scripts/UI/UIManager.cs
2025-09-04 15:29 | refactor | 캔버스를 UIManager 자식으로 통합한 전제에 맞춰 Ensure/동적 재탐색 로직 제거 | Assets/Scripts/UI/UIManager.cs
2025-09-04 15:32 | fix | 다시하기 클릭 시 즉시 게임오버 패널 숨김 처리로 잔존 UI 문제 해결 | Assets/Scripts/UI/UIManager.cs
2025-09-04 15:46 | docs | 소프트 리스타트 전환 리팩터링 계획서 추가 | SOFT_RESTART_REFACTOR_PLAN.md
2025-09-04 15:52 | feat | 소프트 리스타트 도입: GameManager.SoftRestartAsync 추가 및 UI 다시하기 경로 전환 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/UI/UIManager.cs
2025-09-04 15:52 | fix | ObjectSpawner.Initialize에서 플레이어 중심 전환 이벤트 재구독(소프트 리스타트 후 첫 신호 수신 보장) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-04 15:57 | feat | 소프트 리스타트 시 플레이어 공전 중심을 지구로, 지구를 (0,0)으로 이동하도록 초기화 | Assets/Scripts/Player/PlayerController.cs,Assets/Scripts/Game/GameManager.cs
2025-09-04 15:59 | fix | 소행성 이동을 GameManager가 Playing 상태일 때만 수행하도록 제한 | Assets/Scripts/Game/Asteroid.cs
2025-09-04 16:11 | feat | 플레이어 공전 원 내부 금지 규칙에 orbitGap(지구/태양 크기 고려) 반영 및 직렬화 필드 추가 | Assets/Scripts/Game/ObjectSpawner.cs

2025-09-05 01:19 | fix | 장애물 소행성/슈팅스타와 플레이어 충돌 시 라이프 1 감소 처리 추가 | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-05 01:21 | feat | 플레이어 반지름 기본/최대값(1.5/3.0) 및 자연 감소(초당 0.05), 소행성 파괴 시 +0.2 증가 로직 추가 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Player/PlayerController.cs
2025-09-05 01:30 | fix | 피격 처리 변경: 반지름이 1.5 초과 시 라이프/허트 미적용, 반지름만 감소(0.2); 1.5 이하에서만 라이프 감소 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-05 01:41 | fix | 블랙홀 충돌 판정 개선: 공전 중심이 블랙홀 트리거에 실제 진입했을 때만 게임오버(OverlapPoint) | Assets/Scripts/Game/BlackHole.cs
2025-09-05 01:41 | feat | UIManager에 목숨수 텍스트 표시/동기화 추가(OnLivesChanged 구독, SetLivesText API) | Assets/Scripts/UI/UIManager.cs
2025-09-05 01:51 | refactor | 목숨 시스템 제거 및 반지름 임계 기반 게임오버 도입(최초 파괴 전 최소 1.5, 파괴 후 최소 0.8 인스펙터 설정 가능) | Assets/Scripts/Game/GameManager.cs
2025-09-05 01:51 | fix | 장애물/슈팅스타 충돌 시 Hurt/라이프 처리 제거, 반지름/게임오버 처리만 수행 | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-05 01:51 | refactor | UIManager의 목숨 텍스트/이벤트 연동 제거 | Assets/Scripts/UI/UIManager.cs
2025-09-05 01:56 | fix | 장애물 소행성/슈팅스타 충돌 시 Hurt 애니메이션 재생 추가 | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-05 01:56 | feat | 슈팅스타 발사 지연 추가(라인 먼저 표시 후 인스펙터 설정 시간 경과 뒤 발사) | Assets/Scripts/Game/ShootingStar.cs
2025-09-05 01:19 | fix | 장애물 소행성/슈팅스타와 플레이어 충돌 시 라이프 1 감소 처리 추가 | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ShootingStar.cs

2025-09-05 12:27 | feat | 플레이어 이동 가능 범위 LineRenderer 시각화 컴포넌트 추가 | Assets/Scripts/Player/MovementRangeVisualizer.cs
2025-09-05 12:31 | feat | 게임오버 경계(스폰 반경) LineRenderer 시각화 추가 | Assets/Scripts/Game/GameOverBoundaryVisualizer.cs
2025-09-05 12:35 | refactor | 스폰 반경 Gizmo 제거(런타임 LineRenderer로 대체) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-05 12:43 | feat | GameOverBoundaryVisualizer 에디터 표시 지원([ExecuteAlways], OnValidate) | Assets/Scripts/Game/GameOverBoundaryVisualizer.cs
2025-09-05 12:44 | fix | 에디터/런타임 반경 불일치 수정(에디터는 카메라 기반 계산) | Assets/Scripts/Game/GameOverBoundaryVisualizer.cs

2025-09-05 15:38 | fix | GameOverPanel Awake에서 즉시 Hide 제거로 최초 활성화 시 패널 비표시 버그 수정 | Assets/Scripts/UI/GameOverPanel.cs
2025-09-05 19:33 | feat | 더블 스코어 모드 컨트롤러/패널 스크립트 추가 | Assets/Scripts/Game/DoubleScoreModeController.cs,Assets/Scripts/UI/DoubleScorePanel.cs
2025-09-05 19:33 | feat | Player 오빗 속도 오버라이드 API 추가 | Assets/Scripts/Player/PlayerController.cs
2025-09-05 19:33 | refactor | GameManager에 더블 스코어/감쇠 억제/거리 고정 통합 | Assets/Scripts/Game/GameManager.cs
2025-09-05 18:52 | docs | 더블 스코어 모드 계획에서 Stardust 관련 내용 전면 제거 | DOUBLE_SCORE_MODE_PLAN.md
2025-09-05 17:38 | docs | 계획 문서 내 DoubleScoreUI 명칭을 DoubleScorePanel로 정정 | DOUBLE_SCORE_MODE_PLAN.md
2025-09-05 17:29 | docs | 더블 스코어 모드 구현 계획 문서 추가 | DOUBLE_SCORE_MODE_PLAN.md
2025-09-05 17:02 | fix | UI 버튼 클릭을 입력에서 제외하도록 Player 탭 판정 개선 | Assets/Scripts/Player/PlayerController.cs

2025-09-05 20:05 | fix | GameOverPanel 점수 표기를 천 단위 콤마 형식으로 변경 | Assets/Scripts/UI/GameOverPanel.cs
2025-09-05 23:31 | fix | 소프트 리스타트 시 Ready 상태 유지(즉시 시작 방지) | Assets/Scripts/Game/GameManager.cs
2025-09-05 23:31 | feat | 상단 좌측 버튼 컨트롤러 추가(오디오/진동 토글 비워둠, 재시작 구현 및 Playing에서만 활성) | Assets/Scripts/UI/TopLeftButtons.cs
2025-09-05 23:33 | refactor | SoftRestart 동작 복원 및 Ready 전용 API 추가(RestartToReadyAsync) | Assets/Scripts/Game/GameManager.cs
2025-09-05 23:33 | chore | TopLeftButtons 재시작 호출 API를 RestartToReadyAsync로 변경 | Assets/Scripts/UI/TopLeftButtons.cs
2025-09-05 23:41 | chore | Assets/Audio 폴더 메타 제외 전부 .gitignore 추가 | .gitignore

2025-09-06 00:22 | docs | AudioManager 구현 계획 문서 추가 | AUDIO_MANAGER_PLAN.md
2025-09-06 00:32 | feat | AudioManager 기본 구현 및 오디오 토글 연동 | Assets/Scripts/Utilities/AudioManager.cs,Assets/Scripts/UI/TopLeftButtons.cs
2025-09-06 00:42 | feat | 오디오 토글 버튼 아이콘 상태 동기화(음소거 이벤트 구독) | Assets/Scripts/Utilities/AudioManager.cs,Assets/Scripts/UI/TopLeftButtons.cs
2025-09-06 00:49 | fix | Main/Game 씬 진입 시 기본 BGM 자동 재생 옵션 추가 | Assets/Scripts/Utilities/AudioManager.cs
2025-09-06 01:03 | feat | SFX 카탈로그/열거형 도입 및 PlaySfx(SfxId) 추가 | Assets/Scripts/Utilities/SfxId.cs,Assets/Scripts/Utilities/SfxCatalog.cs,Assets/Scripts/Utilities/AudioManager.cs
2025-09-06 01:26 | refactor | SFX를 문자열 키 기반으로 전환 및 PlaySfx(string) 도입 | Assets/Scripts/Utilities/SfxCatalog.cs,Assets/Scripts/Utilities/AudioManager.cs

2025-09-06 02:17 | feat | 타이틀 이미지 추가 | Assets/Sprites/AIGen/UI/Generated Image September 06, 2025 - 1_51AM.png,Assets/Sprites/AIGen/UI/Generated Image September 06, 2025 - 1_51AM.jpeg
2025-09-06 13:21 | fix | 슈팅스타 중복 충돌 방지(플래그+콜라이더 비활성화) | Assets/Scripts/Game/ShootingStar.cs
2025-09-06 13:21 | feat | 소행성 수 기반 스폰 간격 동적 조정(기준 미만 시 1/2) | Assets/Scripts/Game/ObjectSpawner.cs
2025-09-06 14:02 | feat | 블랙홀 스폰 시 페이드 인 애니메이션 추가 | Assets/Scripts/Game/BlackHole.cs
2025-09-06 15:09 | feat | 블랙홀 디스폰 시 despawn 트리거 후 애니메이션 완료 대기 | Assets/Scripts/Utilities/GameConstants.cs,Assets/Scripts/Game/ObjectSpawner.cs
2025-09-06 15:12 | feat | 블랙홀 플레이어 흡인(반경/속도 선형 증가) 재구현 | Assets/Scripts/Game/BlackHole.cs
2025-09-06 15:23 | refactor | 블랙홀 Attract 반경 제거 및 항상 플레이어 끌어당김 | Assets/Scripts/Game/BlackHole.cs
2025-09-06 15:24 | fix | 흡인 기준을 플레이어 오브젝트에서 '회전 중심'으로 변경 | Assets/Scripts/Game/BlackHole.cs
2025-09-06 15:35 | fix | 블랙홀 Despawn 중에는 흡인 중단(애니메이터 상태 태그 감지) | Assets/Scripts/Game/BlackHole.cs

2025-09-06 16:58 | feat | 소행성 폭발 시작 시 SFX 재생(키: Explode) 추가 | Assets/Scripts/Game/Asteroid.cs
2025-09-06 17:25 | feat | 플레이어가 장애물 소행성/슈팅스타 충돌 시 Hurt SFX 재생 | Assets/Scripts/Game/Asteroid.cs,Assets/Scripts/Game/ShootingStar.cs
2025-09-06 17:26 | feat | 더블 스코어 모드 활성화 시 SFX 재생(키: OnDoubleScore) 추가 | Assets/Scripts/Game/DoubleScoreModeController.cs
2025-09-06 18:04 | feat | 게임 오버 시 SFX 재생(키: GameOver) 추가 | Assets/Scripts/Game/GameManager.cs
2025-09-06 18:05 | feat | 블랙홀 흡인 SFX 추가: 흡인 중 반복 재생 및 Despawn 시 중지 | Assets/Scripts/Game/BlackHole.cs,Assets\Scripts\Game\ObjectSpawner.cs
2025-09-06 17:14 | feat | 플레이어 중심 전환 시 SFX 재생(키: SwapCenter) 추가 | Assets/Scripts/Player/PlayerController.cs
2025-09-06 18:32 | refactor | 블랙홀 Despawn SFX를 애니메이션 이벤트로 재생하도록 변경 | Assets/Animation/\_Object/Blackhole_Despawn.anim,Assets/Animation/\_Object/Blackhole.controller

2025-09-06 18:40 | refactor | 씬 구분 없이 하나의 DefaultBGM만 자동 재생하도록 통일 | Assets/Scripts/Utilities/AudioManager.cs
2025-09-06 19:24 | feat | 슈팅스타 이동 중 버닝 SFX 반복 재생 추가(키: ShootingStarBurn) | Assets/Scripts/Game/ShootingStar.cs

2025-09-06 21:11 | feat | DataManager/진동 매니저 기본 구현 및 오디오 연동 | Assets/Scripts/Utilities/DataManager.cs,Assets/Scripts/Utilities/VibrationManager.cs,Assets/Scripts/Utilities/AudioManager.cs
2025-09-06 21:30 | feat | 게임 종료/앱 일시중지·종료 시 데이터 저장 로직 추가 | Assets/Scripts/Game/GameManager.cs,Assets/Scripts/Utilities/DataManager.cs
2025-09-06 21:45 | feat | GameOverPanel: 최고 점수 텍스트 갱신 메소드 추가 | Assets/Scripts/UI/GameOverPanel.cs
2025-09-06 21:52 | feat | 상단 좌측 버튼에 진동 On/Off 토글 구현(DataManager/VibrationManager 연동) | Assets/Scripts/UI/TopLeftButtons.cs

2025-09-08 17:06 | chore | Burst 디버그 정보 폴더 .gitignore 추가(개별/패턴) | .gitignore
2025-09-08 17:11 | docs | README 프로젝트 내용 갱신(영상/저작권 제외) | README.md
2025-09-08 17:28 | docs | README 주요 기능 ‘오브젝트 스폰’ 항목 표현 보강 | README.md

2025-09-08 17:52 | refactor | 입력 시스템 신(Input System)으로 통일: PlayerController/GameManager 입력 처리 마이그레이션 | Assets/Scripts/Player/PlayerController.cs,Assets/Scripts/Game/GameManager.cs
2025-09-08 17:52 | chore | Active Input Handling을 New로 설정(ProjectSettings) | ProjectSettings/ProjectSettings.asset
2025-09-08 18:17 | feat | 소행성 충돌 시 진동 추가(장애물=중간, 일반=짧게) | Assets/Scripts/Game/Asteroid.cs
2025-09-08 20:20 | fix | 블랙홀 게임오버 판정 Enter/Stay 공통화 및 디스폰 가드 추가 | Assets/Scripts/Game/BlackHole.cs
2025-09-08 20:40 | feat | 슈팅스타 충돌 시 진동 재생 추가(장애물=중간 강도) | Assets/Scripts/Game/ShootingStar.cs
2025-09-08 20:42 | chore | 루트 .utmp 로컬 폴더 .gitignore에 추가 | .gitignore
2025-09-08 20:46 | chore | Unity 백업 폴더 .gitignore에 추가(특정/패턴) | .gitignore

2025-09-08 21:19 | refactor | 오디오 음소거 토글을 DataManager 경유로 일원화 및 진동 아이콘 설정 변경 이벤트 동기화 | Assets/Scripts/UI/TopLeftButtons.cs
2025-09-08 21:24 | refactor | HandleDataSettingsChanged에서 AudioManager 의존 제거, DataManager 상태로 아이콘 갱신 | Assets/Scripts/UI/TopLeftButtons.cs
2025-09-08 21:31 | refactor | TopLeftButtons 사용자 수정 반영: 오디오/진동 토글 DataManager 경유 및 아이콘 갱신 로직 정리 | Assets/Scripts/UI/TopLeftButtons.cs
2025-09-08 21:31 | refactor | 불필요한 이벤트 제거: OnBestScoreChanged | Assets\Scripts\Utilities\DataManager.cs

<!-- COMMIT_BOOKMARK -->

2025-09-08 21:36 | fix | 음소거 해제 시 SFX가 몰아서 재생되던 현상 수정 — AudioListener.pause 제거, SFX 재생 가드 추가 | Assets/Scripts/Utilities/AudioManager.cs
