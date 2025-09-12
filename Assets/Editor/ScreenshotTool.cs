using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 스크린샷 캡처 에디터 유틸리티 (단순 버전)
// - ScreenCapture.CaptureScreenshot 사용
// - 게임 뷰 해상도를 그대로 캡처
// - 저장 위치: 프로젝트 루트(Assets 상위 폴더)
// - 메뉴: Tools/Capture Screenshot (GameView Simple)
public static class ScreenshotTool
{
    [MenuItem("Tools/Capture Screenshot (GameView Simple)")]
    public static void Capture()
    {
        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(projectRoot, fileName);

            // 게임 뷰의 현재 해상도를 그대로 캡처
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"스크린샷 캡처 요청됨(게임 뷰 해상도 그대로): {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"스크린샷 캡처 중 오류 발생: {ex.Message}");
        }
    }
}

