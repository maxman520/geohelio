using UnityEngine;

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
    }
}
