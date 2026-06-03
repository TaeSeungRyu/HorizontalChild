using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// M3 — Natural Earth 텍스처를 SeaPlane 에 매핑해 세계지도 배경 생성.
    ///
    /// Equirectangular 투영이라 우리 GeoCoordinate (x=경도, z=위도) 좌표계와
    /// 별도 UV 변환 없이 그대로 일치한다.
    ///   - 이미지 좌하단(0,0) = 경도 -180, 위도 -90 → Plane (-x,-z)
    ///   - 이미지 우상단(1,1) = 경도 +180, 위도 +90 → Plane (+x,+z)
    ///
    /// 동작:
    ///   1) Assets/Game/Art/Map/HYP_LR_SR_W.tif 의 Import Settings 강제
    ///      (Wrap Clamp / sRGB / Max 2048 / ASTC 6x6 모바일)
    ///   2) Assets/Game/Art/Map/WorldMap.mat 생성/갱신 (URP Unlit + 텍스처)
    ///   3) 현재 씬에 SeaPlane GameObject 생성 (Plane, Scale 540×1×270, y=-0.05)
    ///   4) 기존 SeaPlane 있으면 Transform/Material 만 갱신
    /// </summary>
    public static class M3WorldMapSeeder
    {
        private const string TexturePath = "Assets/Game/Art/Map/HYP_LR_SR_W.tif";
        private const string MaterialPath = "Assets/Game/Art/Map/WorldMap.mat";
        private const string SeaPlaneName = "SeaPlane";

        // GeoCoordinate.WorldWidthUnits 와 일치해야 함 (현재 5400).
        // Plane primitive 기본 크기 10 → 스케일 540.
        private const float WorldWidthUnits = 5400f;
        private const float PlaneScaleX = WorldWidthUnits / 10f;          // 540
        private const float PlaneScaleZ = WorldWidthUnits / 10f * 0.5f;   // 270
        private const float SeaY = -0.05f;                                 // 큐브와 z-fight 방지

        [MenuItem("Game/Setup World Map (Sea Plane)")]
        public static void SetupWorldMap()
        {
            var texture = ApplyTextureImportSettings();
            if (texture == null)
            {
                EditorUtility.DisplayDialog(
                    "World Map Setup",
                    $"텍스처를 찾을 수 없습니다:\n{TexturePath}\n\nNatural Earth 의 HYP_LR_SR_W.tif 를 해당 경로에 넣어주세요.",
                    "OK");
                return;
            }

            var material = CreateOrUpdateMaterial(texture);
            CreateOrUpdateSeaPlane(material);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(
                "[M3WorldMapSeeder] 완료.\n" +
                $"  • 텍스처: {TexturePath} (sRGB, Clamp, Max 2048)\n" +
                $"  • 머티리얼: {MaterialPath} (URP Unlit)\n" +
                $"  • SeaPlane: pos (0, {SeaY}, 0), scale ({PlaneScaleX}, 1, {PlaneScaleZ})\n" +
                "다음 단계: 카메라 pitch 20~30°, 큐브 알파 조정 (선택).");
        }

        // ─── Texture Import ────────────────────────────────────────────────

        private static Texture2D ApplyTextureImportSettings()
        {
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null) return null;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
            if (importer.sRGBTexture != true) { importer.sRGBTexture = true; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (importer.mipmapEnabled != true) { importer.mipmapEnabled = true; dirty = true; }
            if (importer.isReadable) { importer.isReadable = false; dirty = true; }

            // 데스크탑 기본
            var defaultSettings = importer.GetDefaultPlatformTextureSettings();
            if (defaultSettings.maxTextureSize != 2048)
            {
                defaultSettings.maxTextureSize = 2048;
                defaultSettings.format = TextureImporterFormat.Automatic;
                defaultSettings.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(defaultSettings);
                dirty = true;
            }

            // 안드로이드 (모바일) — ASTC 6x6
            var android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.maxTextureSize != 2048 || android.format != TextureImporterFormat.ASTC_6x6)
            {
                android.overridden = true;
                android.maxTextureSize = 2048;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(android);
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        // ─── Material ──────────────────────────────────────────────────────

        private static Material CreateOrUpdateMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                // URP 가 없으면 fallback
                shader = Shader.Find("Unlit/Texture");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                EnsureFolder(Path.GetDirectoryName(MaterialPath));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            // URP Unlit 의 메인 텍스처는 "_BaseMap"
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            else if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        // ─── SeaPlane GameObject ───────────────────────────────────────────

        private static void CreateOrUpdateSeaPlane(Material material)
        {
            var existing = GameObject.Find(SeaPlaneName);
            GameObject plane;
            if (existing != null)
            {
                plane = existing;
            }
            else
            {
                plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = SeaPlaneName;
                Undo.RegisterCreatedObjectUndo(plane, "Create SeaPlane");
            }

            plane.transform.position = new Vector3(0f, SeaY, 0f);
            plane.transform.rotation = Quaternion.identity;
            plane.transform.localScale = new Vector3(PlaneScaleX, 1f, PlaneScaleZ);

            // 충돌 비활성 — 배가 평면에 부딪히면 안 됨
            var collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = plane.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            plane.isStatic = true;
            Selection.activeGameObject = plane;
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
