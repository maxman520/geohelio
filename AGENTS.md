# Repository Guidelines

## Agent-Specific Instructions

- Make sure to proceed with reasoning and work in English and answer only the final answer in Korean.
- 비동기 작업 구현 시 UniTask를 활용할 것.
- 주석 및 `Debug.Log()`, `Debug.LogError()`, `Debug.LogWarning()`의 문자열, 깃 커밋 메시지를 한글로 작성할 것.
- `FindObjectOfType` 사용이 필요하다면 대신 `FindFirstObjectByType`(제네릭 포함) 사용으로 대체할 것.
- 단순 질문 답변을 제외한 나머지 코딩, 파일 수정 등의 작업이 끝날 때마다 반드시, 루트의 `WORKLOG.MD`에 동일한 작업 내용을 즉시 기록할 것.
- 커밋 메시지 작성 시 `WORKLOG.MD`의 북마크부터 문서 끝까지의 변경 요약을 활용할 것.
- 커밋 메시지 작성 시 `WORKLOG.MD` 요약을 참고하되, 생성 후 즉시 삭제되었거나 결과적으로 원상복구되어 실질 변화가 없는 작업(예: 임시 파일/코드 추가 후 제거)은 커밋 메시지에 포함하지 말 것.
- 명명 규칙을 엄격 준수하고 불일치 발견 시 즉시 통일할 것:
  - 지역 변수/매개변수: `camelCase`
  - 클래스·메서드·속성·public 멤버: `PascalCase`
  - private 필드: `_camelCase` (직렬화 필드는 `[SerializeField] private camelCase`)
- 다음의 경우 반드시 command approval / change approval 을 묻지 말고 즉시 실행할 것:
  - 파괴적 작업(파일/디렉터리 삭제 등)을 제외한 나머지 작업(파일 read, 폴더 구조 탐색 등).
  - 코딩 작업(스크립트 추가·수정)
  - `WORKLOG.MD` 수정
- 스크립트 생성 시 인코딩은 UTF-8, 줄바꿈은 LF 를 사용한다.

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
