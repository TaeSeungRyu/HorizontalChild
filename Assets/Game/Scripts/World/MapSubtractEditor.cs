using System.Collections.Generic;
using System.IO;
using Game.Data;
using Game.Ship;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.World
{
    /// <summary>
    /// 런타임 맵 카브 에디터 — 사각형 또는 폴리라인을 그려 육지에서 잘라낼 영역을 만든다.
    /// 변경은 즉시 MapSubtractData SO 로 저장. "Re-bake" 누르면 M3WorldMeshBaker 가
    /// 새 메쉬를 굽고 씬의 WorldLand 인스턴스를 즉시 갱신.
    ///
    /// 사용:
    ///   1) Hierarchy 빈 GameObject + 본 컴포넌트 부착.
    ///   2) Inspector 에서 MapSubtractCatalog 할당.
    ///   3) enableOnStart 또는 우클릭 메뉴 → Enable Editor Mode.
    ///
    /// 조작:
    ///   Mode = Rectangle : 좌클릭 드래그 → 사각형 영역. 떼면 SO 저장.
    ///   Mode = Polyline  : 좌클릭 → 점 추가. Enter → 폴리라인 확정 (마우스 휠로 폭 조절).
    ///   기존 영역 클릭 → 선택 (Delete 로 삭제).
    ///   우클릭 드래그 = 카메라 팬, 휠 = 줌.
    ///   M  → Mode 토글.
    ///   B  → Re-bake.
    ///   Esc → 작성 중인 폴리라인 취소.
    ///
    /// 빌드(.exe/.apk) 에선 저장 + bake 부분 동작 안 함 — Editor Play 전용 툴.
    /// </summary>
    public class MapSubtractEditor : MonoBehaviour
    {
        public enum EditMode { Rectangle, Polyline }

        [Header("Refs")]
        public MapSubtractCatalog catalog;
        public Camera mainCamera;
        public ShipController playerShip;

        [Header("Camera")]
        public float panSpeed = 0.5f;
        public float zoomSpeed = 300f;
        public float minCameraY = 30f;
        public float maxCameraY = 3000f;

        [Header("Drawing")]
        [Tooltip("어떤 모양으로 새 영역을 그릴지.")]
        public EditMode mode = EditMode.Rectangle;
        [Tooltip("폴리라인 모드 신규 영역의 기본 폭 (km). 휠로 변경 가능.")]
        [Range(10f, 500f)] public float defaultWidthKm = 50f;
        [Tooltip("폴리라인 모드 폭 조절 단계 (km / 휠 노치).")]
        public float widthStepKm = 5f;

        [Header("Visual")]
        public float visualY = 35f;     // 핸들 Y (NPC 위)
        public float lineWidth = 1.5f;
        public Color polygonOutlineColor = new Color(1f, 0.4f, 0.4f, 0.9f);   // 빨강
        public Color polylineOutlineColor = new Color(0.4f, 0.7f, 1f, 0.9f);  // 파랑
        public Color previewColor = new Color(1f, 1f, 0.3f, 0.9f);            // 노랑
        public Color selectedColor = new Color(0.4f, 1f, 0.4f, 1f);           // 초록
        public TMP_FontAsset labelFont;
        public float labelFontSize = 24f;

        [Header("Behavior")]
        public bool enableOnStart = false;
        [Tooltip("새 영역을 저장할 SO 폴더 (Editor only). 없으면 자동 생성.")]
        public string saveFolder = "Assets/Game/Data/MapSubtracts";

        // ─── 런타임 상태 ───────────────────────────────────────────────────
        private bool _active;
        private readonly List<SubtractView> _views = new();
        private SubtractView _selected;

        // Rectangle 작업 중
        private bool _rectDragging;
        private Vector3 _rectStartWorld;
        private GameObject _rectPreview;
        private LineRenderer _rectPreviewLine;

        // Polyline 작업 중
        private readonly List<Vector3> _polylineWorld = new();
        private GameObject _polylinePreview;
        private LineRenderer _polylinePreviewLine;
        private float _polylineCurrentWidthKm;

        // 카메라 팬
        private Vector3 _camDragLastMouse;
        private bool _camDragging;

        public bool IsActive => _active;

        // ─── 메뉴 / 활성 ───────────────────────────────────────────────────

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (enableOnStart) Enable();
        }

        [ContextMenu("Enable Editor Mode")]
        public void Enable()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[MapSubtractEditor] Play 모드에서만 작동.");
                return;
            }
            if (_active) return;
            _active = true;
            if (playerShip != null) playerShip.LockInput = true;
            SeaSimulation.Pause(this);
            BuildAllViews();
            _polylineCurrentWidthKm = defaultWidthKm;
            Debug.Log($"[MapSubtractEditor] 에디터 ON — Mode={mode}. M=모드토글, B=Re-bake, Esc=취소, Del=삭제.");
        }

        [ContextMenu("Disable Editor Mode")]
        public void Disable()
        {
            bool wasActive = _active;
            _active = false;
            _rectDragging = false;
            _camDragging = false;
            _polylineWorld.Clear();
            _selected = null;
            if (playerShip != null) playerShip.LockInput = false;
            SeaSimulation.Resume(this);
            SeaSimulation.Reset();
            Time.timeScale = 1f;
            ClearAllViews();
            if (_rectPreview != null) Destroy(_rectPreview);
            if (_polylinePreview != null) Destroy(_polylinePreview);
            if (wasActive) Debug.Log("[MapSubtractEditor] 에디터 OFF.");
        }

        private void OnDisable()
        {
            if (Application.isPlaying) Disable();
        }

        // ─── Update ────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_active || mainCamera == null) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 키보드 단축키
            if (keyboard != null)
            {
                if (keyboard.mKey.wasPressedThisFrame) ToggleMode();
                if (keyboard.bKey.wasPressedThisFrame) Rebake();
                if (keyboard.escapeKey.wasPressedThisFrame) CancelInProgress();
                if (keyboard.deleteKey.wasPressedThisFrame) DeleteSelected();
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                    FinalizePolyline();
            }

            Vector2 mousePos = mouse.position.ReadValue();

            // 우클릭 카메라 팬
            if (mouse.rightButton.wasPressedThisFrame)
            {
                _camDragging = true; _camDragLastMouse = mousePos;
            }
            if (mouse.rightButton.wasReleasedThisFrame) _camDragging = false;
            if (_camDragging)
            {
                Vector3 delta = (Vector3)mousePos - _camDragLastMouse;
                _camDragLastMouse = mousePos;
                float s = panSpeed * Mathf.Max(1f, mainCamera.transform.position.y / 100f);
                mainCamera.transform.position += new Vector3(-delta.x * s, 0f, -delta.y * s);
            }

            // 휠 — 카메라 줌 (단, 폴리라인 작업 중이면 폭 조절)
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.001f)
            {
                if (_polylineWorld.Count > 0)
                {
                    _polylineCurrentWidthKm = Mathf.Clamp(
                        _polylineCurrentWidthKm + (wheel / 120f) * widthStepKm, 10f, 500f);
                    UpdatePolylinePreview();
                }
                else
                {
                    var camPos = mainCamera.transform.position;
                    camPos.y = Mathf.Clamp(camPos.y - (wheel / 120f) * zoomSpeed, minCameraY, maxCameraY);
                    mainCamera.transform.position = camPos;
                }
            }

            // 좌클릭 처리
            if (mouse.leftButton.wasPressedThisFrame)
            {
                // 우선 기존 영역 클릭 검사 → 선택
                var picked = PickClosestView(mousePos, 80f);
                if (picked != null)
                {
                    SelectView(picked);
                }
                else if (mode == EditMode.Rectangle)
                {
                    if (TryGetWorldUnderMouse(mousePos, out var w))
                    {
                        _rectStartWorld = w;
                        _rectDragging = true;
                        EnsureRectPreview();
                    }
                }
                else // Polyline
                {
                    if (TryGetWorldUnderMouse(mousePos, out var w))
                    {
                        if (_polylineWorld.Count == 0)
                        {
                            _polylineCurrentWidthKm = defaultWidthKm;
                            EnsurePolylinePreview();
                        }
                        _polylineWorld.Add(new Vector3(w.x, visualY, w.z));
                        UpdatePolylinePreview();
                        Debug.Log($"[MapSubtractEditor] Polyline 점 추가 ({_polylineWorld.Count}). Enter=확정, Esc=취소.");
                    }
                }
            }

            // 사각형 드래그 중 — 미리보기 갱신
            if (_rectDragging && mode == EditMode.Rectangle && _rectPreview != null)
            {
                if (TryGetWorldUnderMouse(mousePos, out var w))
                {
                    UpdateRectPreview(_rectStartWorld, new Vector3(w.x, visualY, w.z));
                }
            }

            // 사각형 떼기 → 저장
            if (_rectDragging && mouse.leftButton.wasReleasedThisFrame)
            {
                _rectDragging = false;
                if (TryGetWorldUnderMouse(mousePos, out var w))
                {
                    FinalizeRectangle(_rectStartWorld, new Vector3(w.x, visualY, w.z));
                }
                if (_rectPreview != null) { Destroy(_rectPreview); _rectPreview = null; }
            }
        }

        // ─── 마우스 → 월드 좌표 ────────────────────────────────────────────

        private bool TryGetWorldUnderMouse(Vector2 screenPos, out Vector3 world)
        {
            world = default;
            var ray = mainCamera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, new Vector3(0f, visualY, 0f));
            if (!plane.Raycast(ray, out float d)) return false;
            world = ray.GetPoint(d);
            return true;
        }

        // ─── Mode 토글 ─────────────────────────────────────────────────────

        public void ToggleMode()
        {
            mode = mode == EditMode.Rectangle ? EditMode.Polyline : EditMode.Rectangle;
            CancelInProgress();
            Debug.Log($"[MapSubtractEditor] Mode → {mode}");
        }

        public void CancelInProgress()
        {
            _rectDragging = false;
            if (_rectPreview != null) { Destroy(_rectPreview); _rectPreview = null; }
            _polylineWorld.Clear();
            if (_polylinePreview != null) { Destroy(_polylinePreview); _polylinePreview = null; }
        }

        // ─── Rectangle 작업 ────────────────────────────────────────────────

        private void EnsureRectPreview()
        {
            if (_rectPreview != null) return;
            _rectPreview = new GameObject("RectPreview");
            _rectPreview.transform.SetParent(transform);
            _rectPreviewLine = _rectPreview.AddComponent<LineRenderer>();
            ConfigureLineRenderer(_rectPreviewLine, previewColor, 5);
        }

        private void UpdateRectPreview(Vector3 a, Vector3 b)
        {
            if (_rectPreviewLine == null) return;
            float y = visualY;
            _rectPreviewLine.positionCount = 5;
            _rectPreviewLine.SetPosition(0, new Vector3(a.x, y, a.z));
            _rectPreviewLine.SetPosition(1, new Vector3(b.x, y, a.z));
            _rectPreviewLine.SetPosition(2, new Vector3(b.x, y, b.z));
            _rectPreviewLine.SetPosition(3, new Vector3(a.x, y, b.z));
            _rectPreviewLine.SetPosition(4, new Vector3(a.x, y, a.z));
        }

        private void FinalizeRectangle(Vector3 a, Vector3 b)
        {
            float minX = Mathf.Min(a.x, b.x), maxX = Mathf.Max(a.x, b.x);
            float minZ = Mathf.Min(a.z, b.z), maxZ = Mathf.Max(a.z, b.z);
            if ((maxX - minX) < 5f || (maxZ - minZ) < 5f)
            {
                Debug.Log("[MapSubtractEditor] 사각형이 너무 작아요 — 무시.");
                return;
            }

            // 월드 corner → lat/lng
            var p1 = GeoCoordinate.WorldToLatLng(new Vector3(minX, 0, minZ));
            var p2 = GeoCoordinate.WorldToLatLng(new Vector3(maxX, 0, minZ));
            var p3 = GeoCoordinate.WorldToLatLng(new Vector3(maxX, 0, maxZ));
            var p4 = GeoCoordinate.WorldToLatLng(new Vector3(minX, 0, maxZ));

            var data = CreateSubtractAsset();
            if (data == null) return;
            data.widthKm = 0f;
            data.points = new[]
            {
                new Vector2(p1.longitude, p1.latitude),
                new Vector2(p2.longitude, p2.latitude),
                new Vector2(p3.longitude, p3.latitude),
                new Vector2(p4.longitude, p4.latitude),
            };
            SaveAsset(data);
            AddViewFor(data);
            Debug.Log($"[MapSubtractEditor] Rectangle 저장: {data.name}");
        }

        // ─── Polyline 작업 ─────────────────────────────────────────────────

        private void EnsurePolylinePreview()
        {
            if (_polylinePreview != null) return;
            _polylinePreview = new GameObject("PolylinePreview");
            _polylinePreview.transform.SetParent(transform);
            _polylinePreviewLine = _polylinePreview.AddComponent<LineRenderer>();
            ConfigureLineRenderer(_polylinePreviewLine, previewColor, 0);
        }

        private void UpdatePolylinePreview()
        {
            if (_polylinePreviewLine == null) return;
            _polylinePreviewLine.positionCount = _polylineWorld.Count;
            for (int i = 0; i < _polylineWorld.Count; i++)
                _polylinePreviewLine.SetPosition(i, _polylineWorld[i]);
            // 폭에 비례한 라인 굵기 (시각화)
            float worldHalf = (_polylineCurrentWidthKm * 0.5f) / GeoCoordinate.KmPerUnit;
            _polylinePreviewLine.startWidth = worldHalf * 2f;
            _polylinePreviewLine.endWidth = worldHalf * 2f;
        }

        private void FinalizePolyline()
        {
            if (_polylineWorld.Count < 2)
            {
                if (_polylineWorld.Count > 0)
                    Debug.Log("[MapSubtractEditor] Polyline 점이 2개 이상 필요해요.");
                return;
            }

            var pts = new Vector2[_polylineWorld.Count];
            for (int i = 0; i < _polylineWorld.Count; i++)
            {
                var ll = GeoCoordinate.WorldToLatLng(_polylineWorld[i]);
                pts[i] = new Vector2(ll.longitude, ll.latitude);
            }

            var data = CreateSubtractAsset();
            if (data == null) return;
            data.widthKm = _polylineCurrentWidthKm;
            data.points = pts;
            SaveAsset(data);
            AddViewFor(data);
            Debug.Log($"[MapSubtractEditor] Polyline 저장: {data.name} ({pts.Length}점, 폭 {_polylineCurrentWidthKm:F0}km)");

            _polylineWorld.Clear();
            if (_polylinePreview != null) { Destroy(_polylinePreview); _polylinePreview = null; }
        }

        // ─── 영역 시각화 (기존 영역) ────────────────────────────────────────

        private class SubtractView
        {
            public MapSubtractData data;
            public GameObject root;
            public LineRenderer line;
            public TextMeshPro label;
        }

        private void BuildAllViews()
        {
            ClearAllViews();
            if (catalog == null || catalog.all == null) return;
            foreach (var d in catalog.all)
            {
                if (d == null) continue;
                AddViewFor(d);
            }
        }

        private void AddViewFor(MapSubtractData data)
        {
            var go = new GameObject($"View_{data.subtractId ?? data.name}");
            go.transform.SetParent(transform);
            var line = go.AddComponent<LineRenderer>();
            bool isPolyline = data.widthKm > 0f;
            ConfigureLineRenderer(line, isPolyline ? polylineOutlineColor : polygonOutlineColor, 0);

            // 폴리곤 또는 폴리라인 — XZ 평면 정점 시각화
            var polys = MapSubtractGeometry.BuildSubtractPolygonsWorld(data);
            int totalPts = 0;
            foreach (var p in polys) totalPts += p.Length + 1; // +1 close
            line.positionCount = totalPts;
            int cursor = 0;
            foreach (var poly in polys)
            {
                for (int i = 0; i < poly.Length; i++)
                {
                    line.SetPosition(cursor++, new Vector3(poly[i].x, visualY, poly[i].y));
                }
                line.SetPosition(cursor++, new Vector3(poly[0].x, visualY, poly[0].y));
            }

            // 라벨 (첫 점 위에)
            if (data.points != null && data.points.Length > 0)
            {
                var lblGO = new GameObject("Label");
                lblGO.transform.SetParent(go.transform);
                var firstWorld = GeoCoordinate.LatLngToWorld(data.points[0].y, data.points[0].x);
                lblGO.transform.position = new Vector3(firstWorld.x, visualY + 5f, firstWorld.z);
                var tmp = lblGO.AddComponent<TextMeshPro>();
                if (labelFont != null) tmp.font = labelFont;
                tmp.text = string.IsNullOrEmpty(data.displayNameKo) ? data.name : data.displayNameKo;
                tmp.fontSize = labelFontSize;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.faceColor = Color.white;
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = Color.black;
                var rt = lblGO.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(300f, 60f);

                _views.Add(new SubtractView { data = data, root = go, line = line, label = tmp });
            }
            else
            {
                _views.Add(new SubtractView { data = data, root = go, line = line });
            }
        }

        private void ClearAllViews()
        {
            foreach (var v in _views) if (v.root != null) Destroy(v.root);
            _views.Clear();
            _selected = null;
        }

        private SubtractView PickClosestView(Vector2 mousePos, float radiusPixels)
        {
            SubtractView best = null;
            float bestDist = radiusPixels;
            foreach (var v in _views)
            {
                if (v == null || v.line == null) continue;
                for (int i = 0; i < v.line.positionCount; i++)
                {
                    Vector3 wp = v.line.GetPosition(i);
                    Vector3 sp = mainCamera.WorldToScreenPoint(wp);
                    if (sp.z <= 0f) continue;
                    float d = Vector2.Distance(new Vector2(sp.x, sp.y), mousePos);
                    if (d < bestDist) { bestDist = d; best = v; }
                }
            }
            return best;
        }

        private void SelectView(SubtractView v)
        {
            if (_selected != null && _selected != v) RestoreColor(_selected);
            _selected = v;
            v.line.startColor = selectedColor;
            v.line.endColor = selectedColor;
            Debug.Log($"[MapSubtractEditor] 선택: {v.data.displayNameKo ?? v.data.name}. Del 로 삭제.");
        }

        private void RestoreColor(SubtractView v)
        {
            bool isPolyline = v.data.widthKm > 0f;
            var c = isPolyline ? polylineOutlineColor : polygonOutlineColor;
            v.line.startColor = c;
            v.line.endColor = c;
        }

        private void DeleteSelected()
        {
            if (_selected == null) return;
            var v = _selected;
            _selected = null;
            string nameForLog = v.data != null ? v.data.name : "(unknown)";

#if UNITY_EDITOR
            // 1) 카탈로그에서 참조 제거
            if (catalog != null && catalog.all != null)
            {
                var list = new List<MapSubtractData>(catalog.all);
                list.Remove(v.data);
                catalog.all = list.ToArray();
                EditorUtility.SetDirty(catalog);
            }
            // 2) SO 에셋 자체 삭제
            var path = AssetDatabase.GetAssetPath(v.data);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.SaveAssets();
#endif
            if (v.root != null) Destroy(v.root);
            _views.Remove(v);
            Debug.Log($"[MapSubtractEditor] 삭제됨: {nameForLog}");
        }

        // ─── LineRenderer 공통 설정 ────────────────────────────────────────

        private void ConfigureLineRenderer(LineRenderer lr, Color color, int initialCount)
        {
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = color;
            lr.endColor = color;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.positionCount = initialCount;
            // URP unlit 머티리얼이 없으면 fallback Sprites/Default
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                lr.material = mat;
            }
        }

        // ─── 저장 (UNITY_EDITOR 한정) ──────────────────────────────────────

        private MapSubtractData CreateSubtractAsset()
        {
#if UNITY_EDITOR
            EnsureFolder(saveFolder);
            var so = ScriptableObject.CreateInstance<MapSubtractData>();
            string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            so.subtractId = $"subtract.{ts}";
            so.displayNameKo = mode == EditMode.Rectangle ? "사각 카브" : "물길";
            so.name = $"MapSubtract_{ts}";
            return so;
#else
            Debug.LogWarning("[MapSubtractEditor] 런타임 빌드에선 저장 불가.");
            return null;
#endif
        }

        private void SaveAsset(MapSubtractData data)
        {
#if UNITY_EDITOR
            string path = $"{saveFolder}/{data.name}.asset";
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            // 카탈로그에 추가 (자동)
            if (catalog != null)
            {
                var list = new List<MapSubtractData>();
                if (catalog.all != null) list.AddRange(catalog.all);
                list.Add(data);
                catalog.all = list.ToArray();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
#endif
        }

#if UNITY_EDITOR
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
#endif

        // ─── Re-bake ───────────────────────────────────────────────────────

        [ContextMenu("Re-bake World Land")]
        public void Rebake()
        {
#if UNITY_EDITOR
            Debug.Log("[MapSubtractEditor] Re-bake 시작 — 몇 초 걸려요.");
            // Game.asmdef → Game.Editor 의존 추가 안 했으므로 menu 실행으로 우회.
            // (M3WorldMeshBaker.Bake 가 [MenuItem("Game/Bake World Land Mesh from GeoJSON")])
            bool ok = EditorApplication.ExecuteMenuItem("Game/Bake World Land Mesh from GeoJSON");
            if (!ok)
            {
                Debug.LogError("[MapSubtractEditor] 메뉴 실행 실패. Unity 메뉴 'Game ▸ Bake World Land Mesh from GeoJSON' 가 있는지 확인.");
                return;
            }

            // 씬의 WorldLand 인스턴스의 MeshCollider 갱신 (PhysX 캐시)
            // MeshFilter 는 sharedMesh asset 의 내용이 in-place 갱신되어 자동 반영.
            // MeshCollider 는 sharedMesh = sharedMesh 재할당해야 PhysX 가 새 토포로지 인식.
            var landmasses = FindObjectsByType<Landmass>(FindObjectsSortMode.None);
            int refreshed = 0;
            foreach (var lm in landmasses)
            {
                var mc = lm.GetComponent<MeshCollider>();
                if (mc != null)
                {
                    var m = mc.sharedMesh;
                    mc.sharedMesh = null;
                    mc.sharedMesh = m;
                    refreshed++;
                }
            }
            Debug.Log($"[MapSubtractEditor] Re-bake 완료. MeshCollider 갱신 {refreshed} 개.");
            BuildAllViews();   // 카탈로그 갱신 반영
#else
            Debug.LogWarning("[MapSubtractEditor] Re-bake 는 Editor 에서만 가능.");
#endif
        }
    }
}
