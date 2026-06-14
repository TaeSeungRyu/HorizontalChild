using System.Collections.Generic;
using System.IO;
using Game.Data;
using Game.Ship;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.World
{
    /// <summary>
    /// 지형 에디터 — 대항해시대 2 풍 맵 다듬기.
    ///
    /// 사용 흐름:
    ///   1) [바다] 또는 [땅] 버튼 클릭 → 해당 모드 활성 (버튼 강조).
    ///   2) 지도 위에서 마우스 오른쪽 버튼 클릭 → 그 자리에 20km 원 카브 (Sea) 또는 새 땅 (Land) 추가.
    ///      연속 클릭 가능. 변경은 색 원으로 표시 (메모리에만 있음).
    ///   3) Enter 키 또는 같은 버튼 다시 클릭 → 모드 해제.
    ///   4) 모드 해제 상태: 우클릭 드래그 = 카메라 팬, 휠 = 줌.
    ///   5) [저장] 버튼 → SO 생성 + 카탈로그 갱신 + 메쉬 재베이크. 변경 확정.
    ///   6) [취소] 버튼 → 메모리에만 있던 변경을 모두 버림.
    ///
    /// Smart Undo:
    ///   - Land 모드에서 기존 Sea 카브 영역에 클릭 → 그 Sea 제거 표시
    ///   - Sea  모드에서 기존 Land 영역에 클릭     → 그 Land 제거 표시
    ///   - 같은 세션의 pending 도 동일하게 토글
    ///
    /// 빌드(.exe/.apk) 에선 저장 + 베이크 부분 동작 안 함 — Editor Play 전용.
    /// </summary>
    public class MapSubtractEditor : MonoBehaviour
    {
        public enum EditMode { None, Sea, Land }

        [Header("Refs")]
        public MapSubtractCatalog catalog;
        public Camera mainCamera;
        public ShipController playerShip;
        public TMP_FontAsset uiFont;   // 한글 라벨용

        [Header("Brush")]
        [Tooltip("브러시 반지름 (km). [ / ] 키로 조절.")]
        [Range(5f, 200f)] public float brushKm = 20f;
        [Tooltip("브러시 원 정점 수 (높을수록 부드러움).")]
        [Range(8, 48)] public int brushSegments = 24;

        [Header("Camera (mode=None 시 동작)")]
        public float panSpeed = 0.5f;
        public float zoomSpeed = 300f;
        public float minCameraY = 30f;
        public float maxCameraY = 3000f;

        [Header("Visual")]
        [Tooltip("브러시·핸들 Y 위치 (배 위)")]
        public float visualY = 35f;
        public float lineWidth = 1.5f;
        public Color seaColor = new Color(0.2f, 0.5f, 1f, 0.95f);
        public Color landColor = new Color(0.85f, 0.55f, 0.25f, 0.95f);
        public Color existingDim = new Color(1f, 1f, 1f, 0.35f);
        public Color markedRemoveColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        public Color brushCursorColor = new Color(1f, 1f, 0.3f, 0.7f);

        [Header("Behavior")]
        public bool enableOnStart = false;
        public string saveFolder = "Assets/Game/Data/MapSubtracts";

        // ─── 런타임 ────────────────────────────────────────────────────────
        private bool _active;
        private EditMode _mode = EditMode.None;

        private class Pending
        {
            public MapEditKind kind;
            public Vector3 worldCenter;
            public float radiusUnits;
            public GameObject visual;
            public LineRenderer line;
        }
        private readonly List<Pending> _pendings = new();

        private class ExistingView
        {
            public MapSubtractData data;
            public GameObject visual;
            public LineRenderer line;
            public bool markedRemove;
        }
        private readonly List<ExistingView> _existing = new();

        // UI
        private Canvas _ui;
        private Button _seaBtn, _landBtn, _saveBtn, _cancelBtn;
        private Image _seaBtnBg, _landBtnBg, _saveBtnBg;
        private TextMeshProUGUI _statusText;

        // 브러시 커서
        private GameObject _brushCursor;
        private LineRenderer _brushLine;

        // 카메라 드래그 (mode=None 시 우클릭 = 팬)
        private Vector3 _camDragLast;
        private bool _camDragging;

        // CameraFollow 잠시 끄기 — 안 끄면 매 프레임 플레이어 배 쪽으로 끌어당김 → 팬 안 됨
        private CameraFollow _cameraFollow;
        private bool _cameraFollowWasEnabled;

        public bool IsActive => _active;

        // ─── 활성/비활성 ───────────────────────────────────────────────────

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

            // CameraFollow 비활성 — 안 끄면 LateUpdate 가 카메라를 배쪽으로 끌어당겨 팬 무효화
            if (mainCamera != null)
            {
                _cameraFollow = mainCamera.GetComponent<CameraFollow>();
                if (_cameraFollow != null)
                {
                    _cameraFollowWasEnabled = _cameraFollow.enabled;
                    _cameraFollow.enabled = false;
                }
            }

            EnsureUI();
            _ui.gameObject.SetActive(true);

            BuildExistingViews();
            EnsureBrushCursor();
            UpdateStatusText();

            Debug.Log("[MapSubtractEditor] 에디터 ON. [바다] 또는 [땅] 누른 뒤 우클릭으로 칠하기, [저장] 으로 확정.");
        }

        [ContextMenu("Disable Editor Mode")]
        public void Disable()
        {
            bool wasActive = _active;
            _active = false;
            _mode = EditMode.None;
            _camDragging = false;
            if (playerShip != null) playerShip.LockInput = false;
            SeaSimulation.Resume(this);
            SeaSimulation.Reset();
            Time.timeScale = 1f;

            // CameraFollow 복원
            if (_cameraFollow != null)
            {
                _cameraFollow.enabled = _cameraFollowWasEnabled;
                _cameraFollow = null;
            }

            ClearAllPendings();
            ClearAllExistingViews();
            if (_brushCursor != null) Destroy(_brushCursor);
            if (_ui != null) _ui.gameObject.SetActive(false);
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

            // ── 키보드: Enter = 모드 해제, [ / ] = 브러시 크기 ──
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    if (_mode != EditMode.None) SetMode(EditMode.None);
                }
                if (keyboard.leftBracketKey.wasPressedThisFrame)
                    brushKm = Mathf.Clamp(brushKm - 5f, 5f, 200f);
                if (keyboard.rightBracketKey.wasPressedThisFrame)
                    brushKm = Mathf.Clamp(brushKm + 5f, 5f, 200f);
            }

            Vector2 mousePos = mouse.position.ReadValue();
            bool overUI = EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();

            // ── 브러시 커서 위치 갱신 ──
            UpdateBrushCursor(mousePos, overUI);

            // ── 우클릭 처리 ──
            //  release 와 drag-while-pressed 는 UI 위 여부와 관계없이 처리해야
            //  드래그 도중 UI 위로 잠시 지나가도 끊기지 않음.

            // 우클릭 release 는 항상 받음
            if (mouse.rightButton.wasReleasedThisFrame) _camDragging = false;

            if (_mode == EditMode.None)
            {
                // 우클릭 press — UI 위가 아닐 때만 드래그 시작
                if (mouse.rightButton.wasPressedThisFrame && !overUI)
                {
                    _camDragging = true;
                    _camDragLast = mousePos;
                }
                // 드래그 중 — UI 위든 아니든 카메라 이동 (시작점만 빈 영역이면 OK)
                if (_camDragging)
                {
                    Vector3 d = (Vector3)mousePos - _camDragLast;
                    _camDragLast = mousePos;
                    float s = panSpeed * Mathf.Max(1f, mainCamera.transform.position.y / 100f);
                    mainCamera.transform.position += new Vector3(-d.x * s, 0f, -d.y * s);
                }
            }
            else
            {
                // mode=Sea/Land → 우클릭 = 지형 수정 (UI 위는 무시)
                if (mouse.rightButton.wasPressedThisFrame && !overUI)
                {
                    if (TryGetWorldUnderMouse(mousePos, out var w))
                    {
                        HandleTerrainClick(w);
                    }
                }
            }

            // ── 휠 = 줌 (mode 와 관계없이) ──
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.001f)
            {
                var camPos = mainCamera.transform.position;
                camPos.y = Mathf.Clamp(camPos.y - (wheel / 120f) * zoomSpeed, minCameraY, maxCameraY);
                mainCamera.transform.position = camPos;
            }

            UpdateStatusText();
        }

        // ─── 모드 토글 ─────────────────────────────────────────────────────

        public void SetMode(EditMode mode)
        {
            // 같은 모드 다시 클릭 → 해제
            if (_mode == mode) mode = EditMode.None;
            _mode = mode;
            _camDragging = false;
            RefreshButtonHighlights();
        }

        private void RefreshButtonHighlights()
        {
            if (_seaBtnBg != null)
                _seaBtnBg.color = _mode == EditMode.Sea
                    ? new Color(0.25f, 0.55f, 0.95f, 1f)
                    : new Color(0.22f, 0.22f, 0.22f, 0.95f);
            if (_landBtnBg != null)
                _landBtnBg.color = _mode == EditMode.Land
                    ? new Color(0.75f, 0.55f, 0.25f, 1f)
                    : new Color(0.22f, 0.22f, 0.22f, 0.95f);
        }

        // ─── 지형 클릭 처리 ────────────────────────────────────────────────

        private void HandleTerrainClick(Vector3 worldPos)
        {
            float r = (brushKm) / GeoCoordinate.KmPerUnit;
            MapEditKind oppKind = _mode == EditMode.Sea ? MapEditKind.Land : MapEditKind.Sea;
            MapEditKind myKind  = _mode == EditMode.Sea ? MapEditKind.Sea  : MapEditKind.Land;

            // 1) Smart undo — 반대 모드의 pending 가 겹치면 그것을 제거
            for (int i = _pendings.Count - 1; i >= 0; i--)
            {
                if (_pendings[i].kind != oppKind) continue;
                if (Vector3.Distance(_pendings[i].worldCenter, worldPos) < r + _pendings[i].radiusUnits)
                {
                    if (_pendings[i].visual != null) Destroy(_pendings[i].visual);
                    Debug.Log($"[MapSubtractEditor] 반대 pending 취소 ({_pendings[i].kind}).");
                    _pendings.RemoveAt(i);
                    return;
                }
            }

            // 2) Smart undo — 기존 SO 중 반대 종류가 클릭 점을 포함하면 제거 표시 토글
            var hitExisting = FindExistingAtPoint(worldPos, oppKind);
            if (hitExisting != null)
            {
                hitExisting.markedRemove = !hitExisting.markedRemove;
                ApplyExistingViewColor(hitExisting);
                Debug.Log($"[MapSubtractEditor] 기존 {hitExisting.data.kind} '{hitExisting.data.displayNameKo}' " +
                    (hitExisting.markedRemove ? "삭제 예약" : "삭제 취소"));
                return;
            }

            // 3) 새 pending 원 추가
            AddPendingCircle(myKind, worldPos, r);
        }

        private ExistingView FindExistingAtPoint(Vector3 worldPos, MapEditKind kind)
        {
            var p = new Vector2(worldPos.x, worldPos.z);
            foreach (var e in _existing)
            {
                if (e.data == null || e.data.kind != kind) continue;
                var polys = MapSubtractGeometry.BuildSubtractPolygonsWorld(e.data);
                if (MapSubtractGeometry.PointInAny(p, polys)) return e;
            }
            return null;
        }

        private void AddPendingCircle(MapEditKind kind, Vector3 center, float radiusUnits)
        {
            var p = new Pending { kind = kind, worldCenter = center, radiusUnits = radiusUnits };
            p.visual = new GameObject($"Pending_{kind}");
            p.visual.transform.SetParent(transform);
            p.line = p.visual.AddComponent<LineRenderer>();
            var color = kind == MapEditKind.Sea ? seaColor : landColor;
            ConfigureLineRenderer(p.line, color);
            DrawCircle(p.line, center, radiusUnits, brushSegments);
            _pendings.Add(p);
        }

        // ─── 기존 SO 시각화 ────────────────────────────────────────────────

        private void BuildExistingViews()
        {
            ClearAllExistingViews();
            if (catalog == null || catalog.all == null) return;
            foreach (var d in catalog.all)
            {
                if (d == null || !d.enabled) continue;
                var go = new GameObject($"Existing_{d.kind}_{d.name}");
                go.transform.SetParent(transform);
                var lr = go.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lr, existingDim);
                DrawDataOutline(lr, d);
                _existing.Add(new ExistingView { data = d, visual = go, line = lr });
            }
        }

        private void ApplyExistingViewColor(ExistingView e)
        {
            if (e.line == null) return;
            var c = e.markedRemove ? markedRemoveColor : existingDim;
            e.line.startColor = c;
            e.line.endColor = c;
        }

        private void ClearAllExistingViews()
        {
            foreach (var e in _existing) if (e.visual != null) Destroy(e.visual);
            _existing.Clear();
        }

        private void ClearAllPendings()
        {
            foreach (var p in _pendings) if (p.visual != null) Destroy(p.visual);
            _pendings.Clear();
        }

        // ─── 브러시 커서 ───────────────────────────────────────────────────

        private void EnsureBrushCursor()
        {
            if (_brushCursor != null) return;
            _brushCursor = new GameObject("BrushCursor");
            _brushCursor.transform.SetParent(transform);
            _brushLine = _brushCursor.AddComponent<LineRenderer>();
            ConfigureLineRenderer(_brushLine, brushCursorColor);
        }

        private void UpdateBrushCursor(Vector2 mousePos, bool overUI)
        {
            if (_brushCursor == null) return;
            if (overUI || _mode == EditMode.None)
            {
                if (_brushLine.positionCount != 0) _brushLine.positionCount = 0;
                return;
            }
            if (!TryGetWorldUnderMouse(mousePos, out var w))
            {
                _brushLine.positionCount = 0;
                return;
            }
            float r = brushKm / GeoCoordinate.KmPerUnit;
            var c = _mode == EditMode.Sea ? seaColor : landColor;
            c.a = 0.6f;
            _brushLine.startColor = c; _brushLine.endColor = c;
            DrawCircle(_brushLine, w, r, brushSegments);
        }

        // ─── 마우스 → 월드 좌표 ────────────────────────────────────────────

        private bool TryGetWorldUnderMouse(Vector2 screenPos, out Vector3 world)
        {
            world = default;
            var ray = mainCamera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, new Vector3(0f, visualY, 0f));
            if (!plane.Raycast(ray, out float d)) return false;
            world = ray.GetPoint(d);
            world.y = visualY;
            return true;
        }

        // ─── LineRenderer 그리기 ───────────────────────────────────────────

        private void ConfigureLineRenderer(LineRenderer lr, Color color)
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

            // 라인 색이 흰색으로 나오는 문제 — URP/Unlit 가 LineRenderer 의 정점 색을 무시함.
            // Sprites/Default 는 vertex color 를 fragment 에서 곱해줘서 색이 살아남.
            // 추가로 머티리얼의 _Color 도 같이 설정 → 두 방식 다 커버.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
                lr.material = mat;
            }
        }

        private void DrawCircle(LineRenderer lr, Vector3 center, float radius, int segments)
        {
            int n = Mathf.Max(8, segments);
            lr.positionCount = n + 1;
            for (int i = 0; i <= n; i++)
            {
                float t = (i / (float)n) * Mathf.PI * 2f;
                var p = new Vector3(
                    center.x + Mathf.Cos(t) * radius,
                    visualY,
                    center.z + Mathf.Sin(t) * radius);
                lr.SetPosition(i, p);
            }
        }

        private void DrawDataOutline(LineRenderer lr, MapSubtractData d)
        {
            var polys = MapSubtractGeometry.BuildSubtractPolygonsWorld(d);
            int total = 0;
            foreach (var p in polys) total += p.Length + 1;
            lr.positionCount = total;
            int cursor = 0;
            foreach (var poly in polys)
            {
                for (int i = 0; i < poly.Length; i++)
                    lr.SetPosition(cursor++, new Vector3(poly[i].x, visualY, poly[i].y));
                lr.SetPosition(cursor++, new Vector3(poly[0].x, visualY, poly[0].y));
            }
        }

        // ─── 저장 / 취소 ───────────────────────────────────────────────────

        public void Save()
        {
#if UNITY_EDITOR
            EnsureFolder(saveFolder);

            int created = 0, removed = 0;

            // 1) 새 pending → SO 생성
            foreach (var p in _pendings)
            {
                var so = ScriptableObject.CreateInstance<MapSubtractData>();
                string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string uniq = $"{ts}_{created:D3}";
                so.subtractId = $"subtract.{p.kind.ToString().ToLower()}.{uniq}";
                so.displayNameKo = p.kind == MapEditKind.Sea ? $"바다 {created + 1}" : $"땅 {created + 1}";
                so.kind = p.kind;
                so.widthKm = 0f;
                so.enabled = true;
                so.points = CircleToLatLngPolygon(p.worldCenter, p.radiusUnits, brushSegments);
                AssetDatabase.CreateAsset(so, $"{saveFolder}/MapSubtract_{p.kind}_{uniq}.asset");
                created++;
            }

            // 2) markedRemove → SO 삭제
            for (int i = _existing.Count - 1; i >= 0; i--)
            {
                if (!_existing[i].markedRemove) continue;
                var path = AssetDatabase.GetAssetPath(_existing[i].data);
                if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
                if (_existing[i].visual != null) Destroy(_existing[i].visual);
                _existing.RemoveAt(i);
                removed++;
            }

            // 3) 카탈로그 재스캔 (폴더의 모든 SO)
            var allFound = new List<MapSubtractData>();
            if (AssetDatabase.IsValidFolder(saveFolder))
            {
                var guids = AssetDatabase.FindAssets("t:MapSubtractData", new[] { saveFolder });
                foreach (var g in guids)
                {
                    var d = AssetDatabase.LoadAssetAtPath<MapSubtractData>(AssetDatabase.GUIDToAssetPath(g));
                    if (d != null) allFound.Add(d);
                }
            }
            if (catalog == null)
            {
                Debug.LogError("[MapSubtractEditor] Catalog 미할당 — 저장 불가.");
                return;
            }
            catalog.all = allFound.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4) Pending 시각 제거 (이제 SO 가 되었으니)
            ClearAllPendings();

            // 5) 메쉬 재베이크
            bool ok = EditorApplication.ExecuteMenuItem("Game/Bake World Land Mesh from GeoJSON");
            if (ok) RefreshLiveMeshColliders();

            // 6) 새 SO 들을 ExistingView 로 다시 빌드
            BuildExistingViews();

            Debug.Log($"[MapSubtractEditor] 저장 완료. 새 영역 +{created}, 제거 -{removed}. " +
                (ok ? "메쉬 재베이크 완료." : "베이크 실패 — 메뉴 'Game ▸ Bake World Land' 수동 실행."));
#else
            Debug.LogWarning("[MapSubtractEditor] 저장은 Editor Play 모드에서만 가능.");
#endif
        }

        public void Cancel()
        {
            int n = _pendings.Count;
            ClearAllPendings();
            int undone = 0;
            foreach (var e in _existing)
            {
                if (e.markedRemove) { e.markedRemove = false; ApplyExistingViewColor(e); undone++; }
            }
            Debug.Log($"[MapSubtractEditor] 취소: pending {n} 개 버림, 삭제 예약 {undone} 개 복원.");
        }

        private Vector2[] CircleToLatLngPolygon(Vector3 center, float radiusUnits, int segments)
        {
            int n = Mathf.Max(8, segments);
            var arr = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = (i / (float)n) * Mathf.PI * 2f;
                var wp = new Vector3(
                    center.x + Mathf.Cos(t) * radiusUnits,
                    0f,
                    center.z + Mathf.Sin(t) * radiusUnits);
                var ll = GeoCoordinate.WorldToLatLng(wp);
                arr[i] = new Vector2(ll.longitude, ll.latitude);
            }
            return arr;
        }

        private void RefreshLiveMeshColliders()
        {
            var landmasses = FindObjectsByType<Landmass>(FindObjectsSortMode.None);
            int refreshed = 0;
            foreach (var lm in landmasses)
            {
                var mc = lm.GetComponent<MeshCollider>();
                if (mc != null)
                {
                    // PhysX 충돌 캐시 강제 무효화 — null 후 재할당 + enabled 토글
                    var m = mc.sharedMesh;
                    mc.sharedMesh = null;
                    mc.sharedMesh = m;
                    mc.enabled = false;
                    mc.enabled = true;
                    refreshed++;
                }
                // MeshFilter 도 강제 재할당 (그래픽 측 캐시)
                var mf = lm.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var m = mf.sharedMesh;
                    mf.sharedMesh = null;
                    mf.sharedMesh = m;
                }
            }
            Debug.Log($"[MapSubtractEditor] MeshCollider 강제 갱신 — {refreshed}개. " +
                "충돌 안 바뀌면 Play 종료 → 재시작.");
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

        // ─── UI 생성 ────────────────────────────────────────────────────────

        private void EnsureUI()
        {
            if (_ui != null) return;

            var canvasGO = new GameObject("MapSubtractEditor_UI");
            canvasGO.transform.SetParent(transform);
            _ui = canvasGO.AddComponent<Canvas>();
            _ui.renderMode = RenderMode.ScreenSpaceOverlay;
            _ui.sortingOrder = 1000;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem 확인 (필수 — UI 클릭 안 먹히는 흔한 원인)
            if (EventSystem.current == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<InputSystemUIInputModule>();
            }

            // 하단 툴바
            var toolbar = new GameObject("Toolbar");
            toolbar.transform.SetParent(_ui.transform, false);
            var trt = toolbar.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 0);
            trt.pivot = new Vector2(0.5f, 0);
            trt.sizeDelta = new Vector2(0, 110);
            trt.anchoredPosition = new Vector2(0, 20);

            var bg = toolbar.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            var hg = toolbar.AddComponent<HorizontalLayoutGroup>();
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.spacing = 15;
            hg.padding = new RectOffset(20, 20, 15, 15);
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;

            (_seaBtn, _seaBtnBg) = CreateBigButton(toolbar.transform, "바다",
                () => SetMode(EditMode.Sea));
            (_landBtn, _landBtnBg) = CreateBigButton(toolbar.transform, "땅",
                () => SetMode(EditMode.Land));
            (_saveBtn, _saveBtnBg) = CreateBigButton(toolbar.transform, "저장", Save,
                new Color(0.2f, 0.55f, 0.2f, 1f));
            CreateBigButton(toolbar.transform, "취소", Cancel,
                new Color(0.55f, 0.2f, 0.2f, 1f));

            // 상단 상태 텍스트
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(_ui.transform, false);
            var srt = statusGO.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1);
            srt.anchorMax = new Vector2(0, 1);
            srt.pivot = new Vector2(0, 1);
            srt.sizeDelta = new Vector2(800, 130);
            srt.anchoredPosition = new Vector2(20, -20);

            var statusBg = statusGO.AddComponent<Image>();
            statusBg.color = new Color(0f, 0f, 0f, 0.5f);

            var lblGO = new GameObject("Text");
            lblGO.transform.SetParent(statusGO.transform, false);
            var lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(15, 10);
            lrt.offsetMax = new Vector2(-15, -10);

            _statusText = lblGO.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) _statusText.font = uiFont;
            _statusText.fontSize = 22;
            _statusText.color = Color.white;
            _statusText.alignment = TextAlignmentOptions.TopLeft;

            RefreshButtonHighlights();
        }

        private (Button, Image) CreateBigButton(Transform parent, string label, System.Action onClick,
            Color? bgOverride = null)
        {
            var btnGO = new GameObject($"Btn_{label}");
            btnGO.transform.SetParent(parent, false);
            var rt = btnGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(170, 80);

            var img = btnGO.AddComponent<Image>();
            img.color = bgOverride ?? new Color(0.22f, 0.22f, 0.22f, 0.95f);

            var le = btnGO.AddComponent<LayoutElement>();
            le.minWidth = 170; le.minHeight = 80;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            // 라벨 — 자식 TMP
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            var lrt = lblGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;

            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            if (uiFont != null) tmp.font = uiFont;
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            return (btn, img);
        }

        private void UpdateStatusText()
        {
            if (_statusText == null) return;
            int pendingSea = 0, pendingLand = 0;
            foreach (var p in _pendings)
            {
                if (p.kind == MapEditKind.Sea) pendingSea++;
                else pendingLand++;
            }
            int markedRemove = 0;
            foreach (var e in _existing) if (e.markedRemove) markedRemove++;
            int totalChanges = pendingSea + pendingLand + markedRemove;

            string modeKo = _mode switch
            {
                EditMode.Sea  => "<color=#5599FF>바다 칠하기</color>",
                EditMode.Land => "<color=#FFB060>땅 칠하기</color>",
                _ => "<color=#888888>모드 없음 (지도 이동)</color>",
            };

            string saveHint = totalChanges > 0
                ? $"<color=#FFDD55><b>⚠ [저장] 버튼을 눌러야 실제 지도에 적용됩니다 ({totalChanges}개 대기 중)</b></color>"
                : "<color=#AAAAAA>우클릭=칠하기, Enter=모드 해제</color>";

            _statusText.text =
                $"<b>모드:</b> {modeKo}  |  <b>브러시:</b> {brushKm:F0} km  ([ ] 키)\n" +
                $"<b>변경:</b> 바다 +{pendingSea}, 땅 +{pendingLand}, 삭제 -{markedRemove}\n" +
                $"<size=18>{saveHint}</size>";

            // 저장 버튼 강조 — 변경사항이 있으면 밝게
            if (_saveBtnBg != null)
            {
                _saveBtnBg.color = totalChanges > 0
                    ? new Color(0.3f, 0.85f, 0.3f, 1f)   // 밝은 초록 — 누르라고 어필
                    : new Color(0.2f, 0.55f, 0.2f, 1f);  // 평소
            }
        }
    }
}
