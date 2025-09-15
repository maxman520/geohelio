# GeoHelio — 2D 캐주얼 아케이드 모바일 게임

## 프로젝트 소개

지구와 태양을 잇는 광선을 회전시키며 소행성을 파괴하고 점수를 획득하는 2D 캐주얼 아케이드 모바일 게임입니다. 화면 탭으로 공전 중심을 지구 ↔ 태양으로 전환합니다. 첫 탭으로 게임이 시작되며, 거리(반지름)를 관리하고 여러 장애물들(유성, 블랙홀, 빨간 소행성)을 피하며 게임 오버 경계를 넘기지 않는 것이 핵심입니다.

## 프로젝트 개요

- 개발 기간: 3주
- 인원/역할: 1인 (기획/프로그래밍/아트)
- 장르/플랫폼: 캐주얼 · 아케이드 / 모바일(iOS/Android)

## 사용 기술

- 엔진: Unity 6000.2.1f1
- 언어/패키지: C#, UniTask 라이브러리
- 버전관리/환경: Git, VSCode
- 그래픽/아트 툴: Aseprite

## 핵심 메커니즘

- 중심 전환: 탭 입력 시 공전 중심이 지구 ↔ 태양으로 전환
- 광선 파괴/점수: 빔에 닿은 소행성 파괴 및 점수 획득, 동일 중심 유지 콤보에 따라 가산
- 거리(반지름) 시스템: 파괴 시 증가, 시간 경과로 자연 감소, 태양과 지구가 만날 시 게임오버
- 경계 판정: 카메라 기반 반경(가로/세로 절반 중 작은 값)으로 게임오버 경계 설정
- 스폰: ObjectSpawner가 스폰 반경·주기·밀도(현재 수 기준)를 중앙 제어 및 풀링
- 더블 스코어 모드: 라운드당 1회 활성화, 지정 시간 동안 다음 효과 적용
  - 점수 보너스(증가폭 20), 플레이어 거리 고정, 거리 감쇠 중지, 공전 속도 상향

## 주요 폴더 구조

- `Assets/Scripts`: 런타임 스크립트
  - `Game`: 게임 진행/스폰 로직 (`GameManager`, `ObjectSpawner`, `Asteroid` 등)
  - `Player`: 플레이어 조작/광선 이펙트 (`PlayerController`, `SunBeamVfx`)
  - `UI`: UI 패널/메뉴 관리 (`UIManager`, `ScorePanel`, `GameOverPanel` 등)
  - `Utilities`: 공통 시스템 유틸 (`AudioManager`, `DataManager`, `AdsManager` 등)
- `Assets/Scenes`: 게임 씬 리소스
- `Assets/Prefabs`: 프리팹 리소스(플레이어/소행성/장애물 등)
- `Assets/Audio`: BGM/SFX 리소스
- `Assets/Animation`: 애니메이션 클립/컨트롤러
- `Assets/Sprites`: 2D 스프라이트 리소스
- `Assets/Shaders & Materials`: 커스텀 셰이더/머티리얼
- `Assets/Editor`: 에디터 전용 스크립트/툴
- `Assets/GoogleMobileAds`: 광고 SDK 관련 리소스
- `Assets/Plugins`: Android/iOS 플러그인
- `Assets/TextMesh Pro`: 폰트 리소스
- `Assets/Screenshots`: 스크린샷 리소스

## 아키텍처 / 구조

- **GameManager**: 게임 상태 관리, 점수/거리 제어, 이벤트 발행
- **PlayerController**: 입력 처리, 중심 전환, 빔 회전/속도 관리
- **ObjectSpawner**: 오브젝트 스폰/풀링, 특수 오브젝트 관리
- **오브젝트(Asteroid/BlackHole/ShootingStar)**: 충돌 판정, 점수/페널티 처리
- **더블 스코어 모드**: 점수 보너스, 거리 고정, UI 게이지
- **UI 레이어**: 게임 패널/메뉴 표시 및 입력 처리
- **유틸리티**: 오디오, 데이터 저장, 광고, 공용 상수 관리
- **시각화/보조**: 게임오버 경계 표시, 빔 파티클 효과

## 배운 점 & 개선 방향

- 배운 점: 궤도 수학, 이벤트 기반 상태 전이, UniTask 기반 비동기 플로우
- 아쉬운 점: 난이도 / 밸런스 조정 필요, 다양한 장애물·아이템 종류 확장 여지, 플레이어 재화 및 광고를 통한 플레이어 재화 지급 미구현
- 개선 계획: 더욱 다양한 아이템/버프 시스템, 장애물 패턴 추가, 플레이어 재화 구현

## 영상 & 스크린샷

- YouTube:
  [![게임 플레이 영상](https://img.youtube.com/vi/dOgZe2Uq0AU/0.jpg)](https://youtube.com/shorts/dOgZe2Uq0AU?feature=share)
- 스크린샷:
  ![스크린샷](./Assets/Screenshots/00.png)
  ![스크린샷](./Assets/Screenshots/01.png)
  ![스크린샷](./Assets/Screenshots/02.png)
  ![스크린샷](./Assets/Screenshots/03.png)
  ![스크린샷](./Assets/Screenshots/04.png)
  ![스크린샷](./Assets/Screenshots/05.png)
  ![스크린샷](./Assets/Screenshots/06.png)

## 외부 에셋 및 리소스 정보

### 폰트

- 던파 비트비트체 - https://df.nexon.com/data/font/dnfbitbitv2

### 스프라이트

- Yet Another Icons by Prinbles - https://prinbles.itch.io/yet-another-icons
- Void - Environment Pack by Foozle - https://foozlecc.itch.io/void-environment-pack
- Space by PiiiXL - https://piiixl.itch.io/space
- 2d PixelArt Stars - https://s-a-t-u-r-n.itch.io/2d-pixelart-stars

### 오디오

- FREE Music Loop Pack for Game Jam and Prototype - https://tubelesshalo.itch.io/free-music-loop-pack
- Fun Casual Sounds - https://assetstore.unity.com/packages/audio/sound-fx/fun-casual-sounds-64048?aid=1011l5f3d&pubref=chrome
