/// <summary>
/// 게임 전역에서 사용하는 문자열/상수를 모아두는 유틸리티 클래스.
/// 태그/레이어/애니메이터 트리거 등 하드코딩 문자열을 중앙화한다.
/// </summary>
public static class GameConstants
{
    public static class Tags
    {
        public const string Player = "Player"; // 플레이어 태그
        public const string Asteroid = "Object"; // 예시: 소행성/오브젝트 태그(필요 시 수정)
    }

    public static class Anim
    {
        // 필요 시 애니메이터 트리거/상태 태그 정의
        public const string ExplodeTrigger = "explode";
        public const string ExplodeStateTag = "Explode";
        public const string PlayerHurtState = "Hurt";

        // 블랙홀 디스폰 연출용 트리거/태그
        public const string BlackHoleDespawnTrigger = "despawn"; // 블랙홀 디스폰 트리거 이름(애니메이터와 동일하게 유지)
        public const string BlackHoleDespawnStateTag = "Despawn"; // 애니메이터 상태 태그(옵션)
    }

    public static class Scenes
    {
        public const string Main = "MainScene";
        public const string Game = "GameScene";
    }

    public static class SFX
    {
        // SfxCatalog.asset에서 관리되는 키들을 상수로 노출.
        // 카탈로그 키를 추가/변경했다면 여기도 함께 반영할 것.
        public const string OnClickBtn = "OnClickBtn";           // 버튼 클릭
        public const string BlackholePull = "BlackholePull";     // 블랙홀 흡인 루프
        public const string BlackholeDespawn = "BlackholeDespawn"; // 블랙홀 디스폰
        public const string Explode = "Explode";                 // 소행성 폭발
        public const string SwapCenter = "SwapCenter";           // 중심 전환
        public const string Hurt = "Hurt";                       // 플레이어 피해
        public const string GameOver = "GameOver";               // 게임 오버
        public const string OnDoubleScore = "OnDoubleScore";     // 더블 스코어 시작
        public const string ShootingStarStart = "ShootingStarStart"; // 슈팅스타 시작(발사)
        public const string ShootingStarBurn = "ShootingStarBurn";   // 슈팅스타 버닝 루프
    }
}
