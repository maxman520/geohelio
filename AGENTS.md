# Repository Guidelines

## Agent-Specific Instructions

- Reasoning 및 작업 과정은 **영어**로 수행하되, **최종 답변은 한국어**로 작성한다.
- 비동기 작업은 반드시 **UniTask**를 활용한다.
- 주석, `Debug.Log()`, `Debug.LogError()`, `Debug.LogWarning()`의 문자열, 그리고 깃 커밋 메시지는 **한글로 작성**한다.
- `FindObjectOfType` 사용이 필요할 경우, 반드시 `FindFirstObjectByType`(제네릭 포함)으로 대체한다.

### Worklog 작성 규칙

- 단순 질문 답변을 제외한 **코딩, 파일 수정 등 모든 작업이 완료될 때마다** 루트의 `WORKLOG.MD`에 즉시 기록하며 기록 위치는 **북마크(`<!-- COMMIT_BOOKMARK -->`)의 밑에서부터 맨 아래 줄** 이다.
- 작성 형식:

  ```
  YYYY-MM-DD HH:MM | 타입 | 요약 | 파일1,파일2
  ```

  - 시간: 24시간 표기, 로컬 시간 기준
  - 타입: Conventional Commits 권장 (feat, fix, chore, docs, refactor, test 등)
  - 파일은 콤마로 구분하며 경로는 리포지토리 루트 기준 상대경로 사용

### Commit 연계 규칙

- 커밋 메시지는 `WORKLOG.MD`의 **북마크(`<!-- COMMIT_BOOKMARK -->`) 이후 \~ 문서 끝까지의 변경 요약**을 활용한다.
- 커밋 완료 후 북마크를 최신 위치로 이동(또는 훅에 의해 자동 갱신)한다.
- 생성 후 즉시 삭제되었거나 원상복구된 항목(실질 변화 없음)은 커밋 메시지에 포함하지 않는다.

#### 작성 형식 (Conventional Commits 권장)

- 한 줄 요약 (제목)

```
<type>: <summary>
```

- 상세 설명 (필요 시)

```
* 구체적인 변경사항1
* 구체적인 변경사항2
```

#### 타입 (Type)

Conventional Commits 권장 타입을 사용한다:

- `feat`: 새로운 기능 추가
- `fix`: 버그 수정
- `docs`: 문서 관련 변경
- `style`: 코드 포맷/스타일 변경 (기능 영향 없음)
- `refactor`: 리팩터링 (버그 수정이나 기능 추가 아님)
- `test`: 테스트 코드 추가/수정
- `chore`: 빌드, 설정, 기타 잡무

#### 예시

```
feat: README.md 추가

* README.md 초안 작성
* Burst 디버그 정보 폴더를 .gitignore에 추가
```

### 코딩 스타일 규칙

- 명명 규칙을 엄격히 준수하며, 불일치 발견 시 즉시 통일한다:

  - 지역 변수/매개변수: `camelCase`
  - 클래스, 메서드, 속성, public 멤버: `PascalCase`
  - private 필드: `_camelCase`
  - `[SerializeField] private` 필드: `camelCase`

- 모든 스크립트는 **UTF-8 인코딩**과 **LF 줄바꿈**을 사용한다.

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
