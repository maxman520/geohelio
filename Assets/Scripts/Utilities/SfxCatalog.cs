using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SFX 카탈로그: 문자열 키 → AudioClip(+기본 볼륨) 매핑을 보관하는 ScriptableObject.
/// - 우클릭 Create 메뉴로 생성하여 프로젝트에서 중앙 관리합니다.
/// - 키는 공백 트림 후 대소문자 무시(OrdinalIgnoreCase)로 인덱싱합니다.
/// </summary>
[CreateAssetMenu(fileName = "SfxCatalog", menuName = "Audio/SFX Catalog")]
public class SfxCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Header("키/클립/기본 볼륨")]
        public string Key = "";                       // 예: "ui.button_click"
        public AudioClip Clip;
        [Range(0f, 1f)] public float DefaultVolume = 1f;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<string, Entry> _index; // 키 표준화 후 저장

    // 외부 접근용(읽기 전용)
    public IReadOnlyList<Entry> Entries => entries;

    /// <summary>
    /// 카탈로그에서 항목을 조회합니다.
    /// </summary>
    public bool TryGetEntry(string key, out Entry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(key)) return false;
        EnsureIndex();
        var norm = NormalizeKey(key);
        return _index.TryGetValue(norm, out entry);
    }

    private void EnsureIndex()
    {
        if (_index != null) return;
        _index = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.Key)) continue;
            var norm = NormalizeKey(e.Key);
            _index[norm] = e; // 중복 키는 마지막 항목으로 덮어씀
        }
    }

    private static string NormalizeKey(string key)
    {
        return key?.Trim() ?? string.Empty;
    }

    private void OnValidate()
    {
        // 인스펙터에서 수정 시 인덱스를 재구성하도록 캐시 무효화
        _index = null;
    }
}
