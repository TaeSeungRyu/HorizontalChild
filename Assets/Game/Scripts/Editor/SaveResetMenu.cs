using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 저장 데이터 초기화 — 에디터 메뉴 전용.
    ///
    /// 메뉴:
    ///   Game ▸ Reset Save Data        — 확인 후 저장 파일 삭제
    ///   Game ▸ Open Save Folder       — 저장 폴더를 탐색기로 열기 (디버그)
    /// </summary>
    public static class SaveResetMenu
    {
        private const string FileName = "save_slot_0.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        [MenuItem("Game/Reset Save Data")]
        public static void ResetSaveData()
        {
            var path = FilePath;

            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    "Reset Save Data",
                    $"저장 파일이 없습니다.\n\n경로: {path}",
                    "OK");
                return;
            }

            // 확인 다이얼로그
            bool proceed = EditorUtility.DisplayDialog(
                "Reset Save Data",
                $"저장 데이터를 삭제할까요?\n\n{path}\n\n게임 다시 실행 시 새 게임으로 시작됩니다.",
                "삭제",
                "취소");

            if (!proceed) return;

            try
            {
                File.Delete(path);
                Debug.Log($"[SaveResetMenu] 저장 데이터 삭제됨: {path}");
                EditorUtility.DisplayDialog("Reset Save Data", "삭제 완료.\n다음 Play 부터 새 게임으로 시작됩니다.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveResetMenu] 삭제 실패: {e}");
                EditorUtility.DisplayDialog("Reset Save Data", $"삭제 실패:\n{e.Message}", "OK");
            }
        }

        [MenuItem("Game/Reset Save Data", true)]
        public static bool ResetSaveDataValidate()
        {
            // 저장 파일 있을 때만 메뉴 활성화
            return File.Exists(FilePath);
        }

        [MenuItem("Game/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            var folder = Application.persistentDataPath;
            if (!Directory.Exists(folder))
            {
                EditorUtility.DisplayDialog("Open Save Folder",
                    $"폴더가 없습니다 (아직 저장된 적 없음):\n{folder}", "OK");
                return;
            }
            EditorUtility.RevealInFinder(folder);
        }
    }
}
