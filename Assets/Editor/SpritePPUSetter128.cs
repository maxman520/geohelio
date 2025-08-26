using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SpritePPUSetter128
{
    private const float TargetPPU = 128f;

    [MenuItem("Tools/Sprites/PPU를 128로 설정")]
    public static void SetPPUTo128()
    {
        var importers = GetSelectedTextureImporters();
        if (importers.Count == 0)
        {
            Debug.LogWarning("선택된 스프라이트(텍스처) 또는 폴더 내부 텍스처가 없습니다.");
            return;
        }

        int changed = 0;
        int skipped = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var importer in importers)
            {
                bool modified = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    modified = true;
                }

                if (!Mathf.Approximately(importer.spritePixelsPerUnit, TargetPPU))
                {
                    importer.spritePixelsPerUnit = TargetPPU;
                    modified = true;
                }

                if (modified)
                {
                    importer.SaveAndReimport();
                    changed++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"완료: {changed}개 변경, {skipped}개 그대로 유지됨.");
    }

    [MenuItem("Tools/Sprites/PPU를 128로 설정", true)]
    private static bool ValidateSetPPUTo128()
    {
        return GetSelectedTextureImporters().Count > 0;
    }

    private static List<TextureImporter> GetSelectedTextureImporters()
    {
        var list = new List<TextureImporter>();
        var guids = Selection.assetGUIDs;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path))
            {
                var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                foreach (var tg in texGuids)
                {
                    var tPath = AssetDatabase.GUIDToAssetPath(tg);
                    var importer = AssetImporter.GetAtPath(tPath) as TextureImporter;
                    if (importer != null)
                        list.Add(importer);
                }
            }
            else
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                    list.Add(importer);
            }
        }
        return list;
    }
}
