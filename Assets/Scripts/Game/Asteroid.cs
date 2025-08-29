using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 소행성 동작: 플레이어와 충돌 시 폭발 애니메이션 재생 후 스포너로 반환(풀링).
/// - 애니메이터에 "explode" 트리거가 있어야 하며, 폭발 상태에 태그 "Explode"가 지정되면 정확한 종료 대기 가능.
/// - 태그가 없으면 대기 시간이 부족할 수 있어 _fallbackExplodeDuration 를 사용.
/// </summary>
public class Asteroid : MonoBehaviour
{
    [Header("참조")]
    [FormerlySerializedAs("_animator")]
    [SerializeField] private Animator animator;
    [FormerlySerializedAs("_collider2D")]
    [SerializeField] private Collider2D col;

    [Header("설정")]
    [FormerlySerializedAs("_explodeTrigger")]
    [SerializeField] private string explodeTrigger = "explode";
    [FormerlySerializedAs("_explodeStateTag")]
    [SerializeField] private string explodeStateTag = "Explode";
    [FormerlySerializedAs("_fallbackExplodeDuration")]
    [Tooltip("애니메이터 상태 태그가 없을 때 대체로 기다릴 폭발 시간(초)")]
    [SerializeField] private float fallbackExplodeDuration = 0.6f;

    private ObjectSpawner _spawner;
    private bool _exploding;

    public void Initialize(ObjectSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (col == null) col = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        // 재사용 대비 초기화
        _exploding = false;
        if (col != null) col.enabled = true;
        if (animator != null)
        {
            animator.ResetTrigger(explodeTrigger);
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void ResetForSpawn()
    {
        _exploding = false;
        if (col != null) col.enabled = true;
        if (animator != null)
        {
            animator.ResetTrigger(explodeTrigger);
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploding) return;
        if (other == null) return;

        // PlayerController 존재 여부로 플레이어 판정
        var pc = other.GetComponent<PlayerController>();
        if (pc == null)
        {
            // 상위/자식에 있는 경우까지 보정
            pc = other.GetComponentInParent<PlayerController>();
        }

        if (pc != null)
        {
            ExplodeAsync().Forget();
        }
    }

    private async UniTaskVoid ExplodeAsync()
    {
        _exploding = true;
        if (col != null) col.enabled = false;

        // 애니메이션 트리거
        if (animator != null && !string.IsNullOrEmpty(explodeTrigger))
        {
            animator.ResetTrigger(explodeTrigger);
            animator.SetTrigger(explodeTrigger);
        }

        // 상태 전이 프레임 반영
        await UniTask.Yield(PlayerLoopTiming.Update);

        float timeout = Mathf.Max(0.1f, fallbackExplodeDuration * 3f);
        float start = Time.time;

        if (animator != null)
        {
            // 태그가 있다면 해당 상태 종료까지 대기, 없으면 fallback
            bool hasExplodeTag = false;
            for (int i = 0; i < animator.layerCount; i++)
            {
                var info = animator.GetCurrentAnimatorStateInfo(i);
                if (info.IsTag(explodeStateTag)) { hasExplodeTag = true; break; }
            }

            if (hasExplodeTag)
            {
                while (Time.time - start < timeout)
                {
                    bool done = true;
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        var info = animator.GetCurrentAnimatorStateInfo(i);
                        if (info.IsTag(explodeStateTag) && info.normalizedTime < 1f)
                        {
                            done = false; break;
                        }
                    }
                    if (done) break;
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(fallbackExplodeDuration));
            }
        }
        else
        {
            await UniTask.Delay(TimeSpan.FromSeconds(fallbackExplodeDuration));
        }

        // 풀로 반환
        _spawner?.Despawn(transform);
    }

    // 충돌 방식이 아닌 트리거(OnTriggerEnter2D)만 사용합니다.
}
