# Repository Guidelines

## Agent-Specific Instructions

- Make sure to proceed with reasoning and work in English and answer only the final answer in Korean.
- 비동기 작업 구현 시 UniTask를 활용할 것.
- 주석 및 `Debug.Log()`, `Debug.LogError()`, `Debug.LogWarning()`의 문자열, 깃 커밋 메시지를 한글로 작성할 것.
- public 변수 선언 시 대문자로, private 변수 선언 시 소문자로 시작하도록 작성할 것.
- `FindObjectOfType` 사용이 필요하다면 대신 `FindFirstObjectByType`(제네릭 포함) 사용으로 대체할 것.
- 단순 질문 답변을 제외한 나머지 코딩, 파일 수정 등의 작업이 끝날 때마다, 루트의 `WORKLOG.MD`에 동일한 작업 내용을 즉시 기록할 것.
 - 깃 커밋 시 `WORKLOG.MD`에 북마크(책갈피)를 남기고, 다음 커밋에서는 그 북마크부터 문서 끝까지의 변경 요약을 바탕으로 커밋 메시지(Conventional Commits)를 작성할 것. 북마크 예: `<!-- COMMIT_BOOKMARK -->` (단독 한 줄). 커밋 직후에는 북마크를 최신 위치로 갱신할 것.
 - 커밋 메시지 작성 시 `WORKLOG.MD` 요약을 참고하되, 생성 후 즉시 삭제되었거나 결과적으로 원상복구되어 실질 변화가 없는 작업(예: 임시 파일/코드 추가 후 제거)은 커밋 메시지에 포함하지 말 것.

## Game Concept

- 플랫폼: Unity 기반 모바일 게임
- 영감: 플래시게임 _Bacteria_
- 컨셉: 박테리아 → 지구와 태양 테마
- 핵심 메커니즘:
  - 탭 입력 시 회전 중심이 교대로 전환
    - 기본: 지구 중심 회전
    - 탭 시: 태양 중심 → 다시 탭 시 지구 중심 복귀. 이하 반복
  - 지구와 태양은 태양빛 광선으로 연결
  - 회전하는 광선으로 소행성 파괴 및 점수 획득
  - 장애물이나, 사용 가능한 아이템 추후 추가할 수도 있음

## Project Structure & Module Organization

- `Assets/`: Unity project source. Place gameplay code under `Assets/Scripts/` and editor utilities under `Assets/Editor/`.
- `Packages/`: Package manifest and embedded packages.
- `ProjectSettings/`: Unity project configuration (do not edit manually unless you know why).
- `UserSettings/`, `Library/`, `Temp/`, `Logs/`: Local/derived; should not be committed.
- Suggested tests layout: `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`.

## Build, Test, and Development Commands

- Open locally: launch Unity Hub and open the repo root.
- EditMode tests (CLI):

  ```sh
  Unity -batchmode -quit -projectPath . \
    -runTests -testPlatform EditMode \
    -logFile Logs/editmode.log \
    -testResults Temp/editmode-results.xml
  ```

- PlayMode tests (CLI): replace `-testPlatform` with `PlayMode`.
- Headless build (example): provide an editor method, e.g. `BuildScripts.BuildWindows`, then run:

  ```sh
  Unity -batchmode -quit -projectPath . \
    -executeMethod BuildScripts.BuildWindows -logFile Logs/build.log
  ```

## Coding Style & Naming Conventions

- C# with 4-space indentation; UTF-8; max line length ~120.
- Naming: Classes/Enums/Methods `PascalCase`; local vars/params `camelCase`; private fields `_camelCase`; serialized private fields `[SerializeField] private ...`.
- One class per file; filename matches class.
- Place editor-only code in `Assets/Editor/`. Use assembly definitions (`.asmdef`) to keep compile times fast.

## Testing Guidelines

- Framework: Unity Test Runner (NUnit). Name test files `*Tests.cs`.
- Keep EditMode tests fast and isolated; move scene/physics tests to PlayMode.
- Aim for meaningful coverage of core gameplay/math utilities; prefer small, deterministic tests.

## Commit & Pull Request Guidelines

- Commits: Conventional Commits style, e.g. `feat: add heliostat alignment solver`, `fix: null check in tracker`.
- PRs: clear description, linked issues, test coverage for new logic, and screenshots/GIFs for visual changes.
- Do not include changes to `Library/`, `Temp/`, `UserSettings/`, or local environment files.

## Security & Configuration Tips

- Never commit API keys or secrets. Use environment variables or a local config not tracked by Git.
- Keep large binaries out of Git; store in `Assets/StreamingAssets/` only when necessary.

# Unity 작업 지침

> 목적: 기능 구현 지시를 받았을 때 **설계부터 시작**하고, Unity 특성(에디터 조작 필요)을 반영해 \*\*Codex(에이전트)\*\*와 **사용자**의 역할을 명확히 나누며, 구조·코드·성능·메모리·라이프사이클 관점까지 포괄하는 일관된 작업 방식과 산출물 템플릿을 제공한다.

---

## 1. 기본 원칙

1. **작업 지시 = 설계부터 시작.** 바로 코드를 쓰지 않고 요구사항 정리 → 구조(Hierarchy) → 데이터/클래스 설계 → 성능/메모리/라이프사이클 검토 → 코드/테스트 순으로 진행한다.
2. **Hierarchy View 구조 설계가 출발점.** 씬/프리팹 배치와 참조가 Unity에서 중요한 진실의 원천이다. 구조 설계에 따라 스크립트의 책임과 의존성이 결정된다.
3. **역할 분담을 문서화.** Codex가 작성할 산출물(설계안, 코드 스텁, 테스트)과 사용자가 에디터에서 수행할 작업(프리팹 생성, 인스펙터 연결, Addressables/Layer/Tag 설정 등)을 명확히 구분한다.
4. **라이프사이클 적합성 우선.** Awake/OnEnable/Start/Update/FixedUpdate/LateUpdate/OnDisable/OnDestroy 호출 시점과 직렬화/초기화 순서를 고려한다.
5. **성능·메모리를 설계 단계에서 함께 본다.** 오브젝트 풀링, 구조적 공유(프리팹/ScriptableObject), Update 최소화, 할당/GC 절감, 배치/드로우콜, Addressables/씬 분할을 함께 검토한다.

---

## 2. 표준 진행 프로토콜

**Step 0. 요구사항 수집**

- 기능 범위, 인풋/아웃풋, UX 흐름, 타깃 플랫폼/프레임레이트, 추정 동시 오브젝트 수, 데이터 영속 여부, 멀티/네트워크 여부, 아트/사운드 의존성.

**Step 1. Hierarchy 구조 초안**

- 씬/프리팹/빈 오브젝트 구조를 트리로 제시하고, 네이밍 규칙과 레이어·태그·정렬 기준(UI 오더/Sorting Layer)까지 포함.
- 각 노드에 **역할**(책임)과 **핵심 컴포넌트**(필수 스크립트/Collider/Renderer 등), **런타임 생성 여부** 표시.

**Step 2. 역할 분담 명세 (Codex vs 사용자)**

- 에디터에서만 가능한 작업(프리팹 제작, 인스펙터 참조 연결, Addressables 마크, Physics Layer Matrix 설정 등)은 **사용자** 담당.
- 로직/데이터/테스트/디버그 툴 스크립트 초안은 **Codex** 담당. 인스펙터에 노출될 `[SerializeField]` 슬롯/컨피그 정의 포함.

**Step 3. 데이터·클래스 설계**

- 주요 클래스 책임, 의존성 방향, ScriptableObject 기반 컨피그/이벤트 채널, 인터페이스/서비스 경계 정의.
- 라이프사이클(생성/활성/비활성/파괴)과 상태 전이를 텍스트 시퀀스로 명시.

**Step 4. 성능/메모리·라이프사이클 체크**

- Update/Coroutine 남용 점검, 풀링 전략, 할당/GC 위험 지점, 대량 인스턴스 처리 방식(Job/Burst 고려 여지 표기는 선택), 씬/어드레서블 로딩 전략.

**Step 5. 코드 & 테스트 산출**

- 코어 스크립트, 컨피그용 SO, 간이 에디터 Gizmo/Debug UI(선택), Unity Test Runner용 Edit/PlayMode 테스트 샘플, 사용법(Setup 가이드) 포함.

---

## 3. 산출물 템플릿

### 3.1 Hierarchy 설계 템플릿

```
Scene: <SceneName>
└── Systems (DontDestroyOnLoad?)
    ├── GameRoot (GameManager)
    ├── Services
    │   ├── AudioService
    │   └── SaveService
    └── EventBus (SO-backed)
└── Gameplay
    ├── Player
    │   ├── Model (SkinnedMeshRenderer)
    │   └── PlayerController [Script]
    ├── Enemies (Runtime Spawned via Pool)
    └── Collectibles (Runtime Spawned via Pool)
└── UI
    ├── HUD (Canvas - Screen Space Overlay)
    └── PauseMenu
```

- **역할/컴포넌트**: 각 노드에 필요한 스크립트·컴포넌트 나열
- **런타임 생성**: Pool/Factory로 관리 여부 표시
- **레이어/태그**: Physics/Render/Sorting 규칙 표기

### 3.2 역할 분담 표

| 항목        | Codex(에이전트)                    | 사용자(에디터)                       |
| ----------- | ---------------------------------- | ------------------------------------ |
| 구조 설계안 | 트리/역할/참조 다이어그램 제시     | 구조 반영해 씬/프리팹 생성           |
| 스크립트    | 클래스/인터페이스/테스트 코드 제공 | 스크립트 컴포넌트 추가·인스펙터 연결 |
| 컨피그      | ScriptableObject 정의/기본값 제안  | SO 에셋 생성·값 조정                 |
| 리소스      | Addressables/경로 규약 제안        | Addressables 마크/그룹 구성          |
| 물리        | 레이어/충돌 행렬 제안              | 프로젝트 세팅에서 적용               |
| 빌드        | 품질/플랫폼 권장치 제안            | PlayerSettings/품질/빌드 프로필 설정 |

### 3.3 클래스 설계 템플릿 (요약)

```
[서비스]
IAudioService: PlaySfx(id), PlayBgm(id), SetVolume(type, value)
ISaveService: Load<T>(key), Save<T>(key, value)

[도메인]
PlayerController : MonoBehaviour
  - 입력 처리 → 이동/상호작용 트리거
Collectible : MonoBehaviour
  - OnTriggerEnter → EventChannel.Raise(Collected(itemId))
InventoryService : ScriptableObject or MonoService
  - Add(itemId), Has(itemId)

[이벤트 채널]
GameEventChannel<T> : ScriptableObject
  - Action<T> OnRaised; Raise(T data)
```

### 3.4 코드 스텁 예시

#### Event Channel (ScriptableObject)

```csharp
using System;
using UnityEngine;

public abstract class GameEventChannelBase<T> : ScriptableObject
{
    public event Action<T> OnRaised;
    public void Raise(T payload) => OnRaised?.Invoke(payload);
}

[CreateAssetMenu(menuName = "Events/ItemCollected")]
public class ItemCollectedChannel : GameEventChannelBase<string> {}
```

#### 간단한 풀링 유틸

```csharp
using System.Collections.Generic;
using UnityEngine;

public class SimplePool<T> where T : Component
{
    private readonly Stack<T> _stack = new();
    private readonly T _prefab;
    private readonly Transform _root;

    public SimplePool(T prefab, Transform root)
    {
        _prefab = prefab; _root = root;
    }

    public T Get()
    {
        var obj = _stack.Count > 0 ? _stack.Pop() : Object.Instantiate(_prefab, _root);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false);
        _stack.Push(obj);
    }
}
```

#### 라이프사이클 가이드 주석 포함 샘플

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    // Awake: 참조 캐싱/비직렬 필드 초기화
    private void Awake() { /* cache components */ }

    // OnEnable: 이벤트 구독
    private void OnEnable() { /* subscribe input/events */ }

    // Start: 다른 오브젝트/씬 로딩 이후 초기화 의존 시
    private void Start() { /* late init depending on scene */ }

    private void Update()
    {
        // 입력/경량 로직 — 고정 물리는 FixedUpdate 사용
        var dx = Input.GetAxisRaw("Horizontal");
        var dz = Input.GetAxisRaw("Vertical");
        transform.Translate(new Vector3(dx, 0, dz) * moveSpeed * Time.deltaTime);
    }

    private void OnDisable() { /* unsubscribe */ }
    private void OnDestroy() { /* release unmanaged/pooled */ }
}
```

### 3.5 성능·메모리 체크리스트

- **Update 최소화**: 가능하면 이벤트/타이머/스케줄러 사용. 다수 객체는 매니저에서 배치 업데이트.
- **할당 회피**: `new`/LINQ/박싱 Hot Path 금지, `StringBuilder`, `List.Clear()` 재사용.
- **Pooling**: 탄환/효과/임시 오브젝트는 풀로 관리. 비활성화 시 이벤트 구독 해제.
- **렌더링**: 배칭 친화적 머티리얼/셰이더, Mesh/SkinnedMesh 비용 고려, UI Rebuild 최소화.
- **메모리**: 텍스처/오디오 임포트 설정, Addressables로 지연 로드/언로드, 대형 에셋 레퍼런스 체인 점검.
- **물리**: 레이어 충돌 행렬 축소, `FixedUpdate` 주기 조정, Overlap/레이캐스트 빈도 제한.

### 3.6 라이프사이클 적합성 치트시트

- **Awake**: 컴포넌트 캐싱, SO/싱글톤 참조, 비직렬 초기화 (씬간 의존 X)
- **OnEnable**: 이벤트/입력 구독, 풀에서 복귀 시 재구독
- **Start**: 다른 오브젝트 준비 이후 필요 초기화
- **Update**: 프레임 기반 가벼운 로직만
- **FixedUpdate**: 물리 업데이트 전용
- **LateUpdate**: 카메라 추적/정리
- **OnDisable/OnDestroy**: 구독 해제, 풀 반환/리소스 해제

---

## 4. 제출 형식(작업 시 마다 이 포맷으로 제공)

1. **요구사항 요약**: 목표/제약/성능 목표/추정 규모
2. **Hierarchy 설계**: 트리 + 각 노드 역할/컴포넌트/런타임 생성 여부/레이어·태그
3. **역할 분담 표**: Codex와 사용자 작업 항목 체크리스트(✓/□)
4. **클래스 설계**: 책임/의존성/이벤트 채널/데이터 모델
5. **성능·메모리·라이프사이클 검토**: 위험 지점·대응 방안
6. **코드 산출물**: .cs 목록, 사용법(세팅 가이드), 테스트 방법

---

## 5. 예시: 간단한 "수집 아이템" 기능

**요구**: 맵에 일정 주기로 아이템이 스폰되고, 플레이어가 닿으면 인벤토리에 기록, HUD에 수량 표시.

**Hierarchy 초안**

```
Gameplay
└── CollectibleSpawner [Script]
UI
└── HUD
    └── InventoryCounter [Script]
Systems
└── Services
    ├── InventoryService [SO or Mono]
    └── ItemCollectedChannel [SO]
```

**역할 분담**

- Codex: `Collectible`, `CollectibleSpawner`, `InventoryService`, `ItemCollectedChannel`, `InventoryCounter` 코드/테스트, SO 타입 정의
- 사용자: `Collectible` 프리팹 제작(콜라이더 Trigger, 시각/사운드), Spawner에 프리팹/파라미터 연결, HUD 텍스트 바인딩, 레이어/태그 적용

**클래스 관계**

- `Collectible` → 충돌 시 `ItemCollectedChannel.Raise(itemId)`
- `InventoryService` ← 채널 구독해 카운트 증가
- `InventoryCounter` ← 서비스 변화 Observe하여 UI 반영

**코드 스니펫 (요약)**

```csharp
public class Collectible : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private ItemCollectedChannel channel;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        channel.Raise(itemId);
        gameObject.SetActive(false); // 풀 반환 권장
    }
}

public class InventoryService : MonoBehaviour
{
    [SerializeField] private ItemCollectedChannel channel;
    private readonly Dictionary<string,int> counts = new();

    private void OnEnable() => channel.OnRaised += OnItem;
    private void OnDisable() => channel.OnRaised -= OnItem;

    private void OnItem(string id)
    {
        counts[id] = counts.TryGetValue(id, out var c) ? c+1 : 1;
        // TODO: 이벤트/UnityEvent로 HUD에 알림
    }
}
```

**성능/메모리 포인트**

- 다량 아이템: `Collectible` 풀링 필수, `OnTriggerEnter` 외 연산 최소화
- HUD 업데이트: 이벤트 기반, 프레임 폴링 금지

---

## 6. 리뷰 체크리스트 (PR 전에 확인)

- [ ] Hierarchy 설계와 실제 씬/프리팹이 일치
- [ ] 인스펙터 노출 필드에 **Null** 참조 없음
- [ ] Update 남용 없음, 풀링 적용 여부 확인
- [ ] SO/서비스/채널의 라이프사이클 및 구독 해제 보장
- [ ] Addressables/리소스 참조가 씬 로드/언로드 시 누수 없이 동작
- [ ] 테스트/데모 씬, 사용법 문서 포함
