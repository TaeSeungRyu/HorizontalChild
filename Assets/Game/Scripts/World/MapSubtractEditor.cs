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
    /// 지형 에디터 — 드래그로 선을 그려 강/땅 영역 생성.
    ///
    /// 사용 흐름:
    ///   1) [강] 또는 [땅] 버튼 클릭 → 모드 활성 (버튼 강조).
    ///   2) 지도 위에서 마우스 오른쪽 버튼을 누르고 드래그 → 손 떼면 선 확정.
    ///      선 두께 = 브러시 km (Inspector / [ ] 키로 조절).
    ///      여러 번 드래그 = 여러 폴리라인 추가 (메모리에만 있음).
    ///   3) Enter 키 또는 같은 버튼 다시 클릭 → 모드 해제.
    ///   4) 모드 해제 상태: 우클릭 드래그 = 카메라 팬, 휠 = 줌.
    ///   5) [저장] 버튼 → SO 생성 + 카탈로그 갱신 + 메쉬 재베이크 + RiverOverlay 갱신.
    ///   6) [취소] 버튼 → 메모리 pending 모두 버림.
    ///
    /// 빌드(.exe/.apk) 에선 저장 + 베이크 부분 동작 안 함 — Editor Play 전용.
    /// </summary>
    public class MapSubtractEditor : MonoBehaviour
    {
        public enum EditMode { None, River, Land }

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
        [Tooltip("[화면맞추기] 버튼이 카메라를 이 Y 로 스냅. 20km 브러시가 또렷이 보이는 줌.")]
        public float fitScreenCameraY = 80f;

        [Header("Visual")]
        [Tooltip("브러시·핸들 Y 위치. Top-down 시점에선 높여도 parallax 없음 — Land(1.75) 위로 충분히 띄워 가림 없도록.")]
        public float visualY = 30f;
        public float lineWidth = 2.5f;
        public Color riverColor = new Color(0.2f, 0.5f, 1f, 0.95f);
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
            public List<Vector3> linePoints;   // 드래그로 그린 폴리라인 (월드 좌표, Y=visualY)
            public float widthUnits;            // 선 두께 (월드 unit)
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
        private Button _riverBtn, _landBtn, _saveBtn, _cancelBtn;
        private Image _riverBtnBg, _landBtnBg, _saveBtnBg;
        private TextMeshProUGUI _statusText;

        // 브러시 커서 (solid Cylinder 디스크)
        private GameObject _brushCursor;
        private Renderer _brushRenderer;
        private Material _brushMaterial;

        // 카메라 드래그 (mode=None 시 우클릭 = 팬)
        private Vector3 _camDragLast;
        private bool _camDragging;

        // 선 그리기 (mode=River/Land 시 우클릭 드래그 = 선)
        private bool _drawing;
        private readonly List<Vector3> _currentDrawPoints = new();
        private GameObject _drawPreview;
        private LineRenderer _drawPreviewLine;
        [Tooltip("드래그 중 점 간 최소 거리 (월드 unit). 작을수록 부드러운 선, 큰 데이터.")]
        public float minDrawPointDistanceUnits = 2f;

        // CameraFollow 잠시 끄기 — 안 끄면 매 프레임 플레이어 배 쪽으로 끌어당김 → 팬 안 됨
        private CameraFollow _cameraFollow;
        private bool _cameraFollowWasEnabled;

        // 에디터 진입 전 카메라 자세 — 종료 시 복원
        private Quaternion _savedCamRotation;
        private Vector3 _savedCamPosition;
        private bool _savedCamState;

        // WorldLand 인스턴스의 Transform — 카브 좌표를 mesh-local 로 정렬할 때 사용.
        // 사용자가 prefab 을 (0,0,0) 외 위치에 두면 click(world) vs 메쉬 vertex(mesh-local)
        // 가 어긋남. InverseTransformPoint 로 보정.
        private Transform _landTransform;

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

                // 카메라 자세 저장 + top-down 시점으로 강제 (클릭 위치 정확성 위해)
                _savedCamRotation = mainCamera.transform.rotation;
                _savedCamPosition = mainCamera.transform.position;
                _savedCamState = true;

                // 현재 카메라 XZ 유지, Y 는 적당히, 회전은 정수직 내려다보기
                var p = mainCamera.transform.position;
                if (p.y < minCameraY) p.y = 200f;   // 너무 낮으면 적당히 위로
                mainCamera.transform.position = p;
                mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // WorldLand 인스턴스 참조 캐싱 — 좌표 변환에 사용
            var landmass = FindAnyObjectByType<Landmass>();
            _landTransform = landmass != null ? landmass.transform : null;
            if (_landTransform != null && _landTransform.position.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[MapSubtractEditor] WorldLand 위치 보정 적용 — pos={_landTransform.position}");
            }

            EnsureUI();
            _ui.gameObject.SetActive(true);

            // RiverOverlay 자동 보장 — 기존 강 데이터가 즉시 메쉬·통과 영역으로 나타남
            var riverOverlay = FindAnyObjectByType<RiverOverlay>();
            if (riverOverlay == null)
            {
                var go = new GameObject("RiverOverlay (Auto)");
                riverOverlay = go.AddComponent<RiverOverlay>();
                riverOverlay.catalog = catalog;
            }
            else if (riverOverlay.catalog == null)
            {
                riverOverlay.catalog = catalog;
                riverOverlay.Refresh();
            }

            BuildExistingViews();
            EnsureBrushCursor();
            UpdateStatusText();

            Debug.Log("[MapSubtractEditor] 에디터 ON. [강] 또는 [땅] 누른 뒤 우클릭 드래그로 선 그리기 → 손 떼면 확정 → [저장].");
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

            // 카메라 자세 복원
            if (_savedCamState && mainCamera != null)
            {
                mainCamera.transform.rotation = _savedCamRotation;
                mainCamera.transform.position = _savedCamPosition;
                _savedCamState = false;
            }

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
            // Canvas UI 위에 있을 때만 true. PhysicsRaycaster 가 잡는 3D 메쉬는 제외.
            // (PortPlacementEditor 가 Main Camera 에 PhysicsRaycaster 를 붙여 놓으면
            //  육지 MeshCollider 까지 "UI" 로 오인되어 클릭이 막히는 문제 차단)
            bool overUI = IsPointerOverCanvasUI(mousePos);

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
                // mode=River/Land → 우클릭 드래그 = 선 그리기

                // 1) 우버튼 press — 선 시작
                if (mouse.rightButton.wasPressedThisFrame && !overUI)
                {
                    if (TryGetWorldUnderMouse(mousePos, out var w))
                    {
                        _drawing = true;
                        _currentDrawPoints.Clear();
                        _currentDrawPoints.Add(new Vector3(w.x, visualY, w.z));
                        EnsureDrawPreview();
                        UpdateDrawPreview();
                    }
                }

                // 2) 드래그 중 — 마우스 이동 시 점 추가
                if (_drawing && mouse.rightButton.isPressed)
                {
                    if (TryGetWorldUnderMouse(mousePos, out var w))
                    {
                        var newPt = new Vector3(w.x, visualY, w.z);
                        var last = _currentDrawPoints[_currentDrawPoints.Count - 1];
                        if (Vector3.Distance(last, newPt) >= minDrawPointDistanceUnits)
                        {
                            _currentDrawPoints.Add(newPt);
                            UpdateDrawPreview();
                        }
                    }
                }

                // 3) 우버튼 release — 선 확정 → pending polyline 추가
                if (_drawing && mouse.rightButton.wasReleasedThisFrame)
                {
                    _drawing = false;
                    if (_currentDrawPoints.Count >= 2)
                    {
                        var kind = _mode == EditMode.River ? MapEditKind.River : MapEditKind.Land;
                        float widthUnits = brushKm / GeoCoordinate.KmPerUnit;
                        AddPendingPolyline(kind, new List<Vector3>(_currentDrawPoints), widthUnits);
                    }
                    else
                    {
                        Debug.Log("[MapSubtractEditor] 선 길이가 너무 짧음 — 무시. 우클릭 드래그로 그려주세요.");
                    }
                    _currentDrawPoints.Clear();
                    if (_drawPreview != null) { Destroy(_drawPreview); _drawPreview = null; }
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
            if (_riverBtnBg != null)
                _riverBtnBg.color = _mode == EditMode.River
                    ? new Color(0.25f, 0.55f, 0.95f, 1f)
                    : new Color(0.22f, 0.22f, 0.22f, 0.95f);
            if (_landBtnBg != null)
                _landBtnBg.color = _mode == EditMode.Land
                    ? new Color(0.75f, 0.55f, 0.25f, 1f)
                    : new Color(0.22f, 0.22f, 0.22f, 0.95f);
        }

        // ─── 드로잉 ────────────────────────────────────────────────────────

        private void EnsureDrawPreview()
        {
            if (_drawPreview != null) return;
            _drawPreview = new GameObject("DrawPreview");
            _drawPreview.transform.SetParent(transform);
            _drawPreviewLine = _drawPreview.AddComponent<LineRenderer>();
            var color = _mode == EditMode.River ? riverColor : landColor;
            color.a = 0.7f;
            ConfigureLineRenderer(_drawPreviewLine, color);
            float w = brushKm / GeoCoordinate.KmPerUnit;
            _drawPreviewLine.startWidth = w;
            _drawPreviewLine.endWidth = w;
            _drawPreviewLine.numCapVertices = 6;
            _drawPreviewLine.numCornerVertices = 6;
        }

        private void UpdateDrawPreview()
        {
            if (_drawPreviewLine == null) return;
            _drawPreviewLine.positionCount = _currentDrawPoints.Count;
            for (int i = 0; i < _currentDrawPoints.Count; i++)
                _drawPreviewLine.SetPosition(i, _currentDrawPoints[i]);
        }

        private void AddPendingPolyline(MapEditKind kind, List<Vector3> points, float widthUnits)
        {
            var p = new Pending { kind = kind, linePoints = points, widthUnits = widthUnits };
            p.visual = new GameObject($"Pending_{kind}_Line");
            p.visual.transform.SetParent(transform);
            p.line = p.visual.AddComponent<LineRenderer>();
            var color = kind == MapEditKind.River ? riverColor : landColor;
            color.a = 0.8f;
            ConfigureLineRenderer(p.line, color);
            p.line.startWidth = widthUnits;
            p.line.endWidth = widthUnits;
            p.line.numCapVertices = 6;
            p.line.numCornerVertices = 6;
            p.line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) p.line.SetPosition(i, points[i]);
            _pendings.Add(p);
            float lenUnits = 0f;
            for (int i = 1; i < points.Count; i++) lenUnits += Vector3.Distance(points[i - 1], points[i]);
            Debug.Log($"[MapSubtractEditor] 새 pending {kind} 선 추가 — 점 {points.Count}개, 길이 {lenUnits:F1}u (≈{lenUnits * GeoCoordinate.KmPerUnit:F0}km), 폭 {widthUnits * GeoCoordinate.KmPerUnit:F0}km. 총 pending {_pendings.Count}개");
        }

        /// <summary>반투명 머티리얼 — solid disk 등에서 사용.</summary>
        private static Material CreateTransparentMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
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
                var baseColor = ColorForKind(d.kind);
                baseColor.a = 0.55f;
                ConfigureLineRenderer(lr, baseColor);
                DrawDataOutline(lr, d);
                _existing.Add(new ExistingView { data = d, visual = go, line = lr });
            }
        }

        private Color ColorForKind(MapEditKind k)
        {
            switch (k)
            {
                case MapEditKind.River: return riverColor;
                case MapEditKind.Land:  return landColor;
                default: return existingDim;
            }
        }

        private void ApplyExistingViewColor(ExistingView e)
        {
            if (e.line == null) return;
            var c = e.markedRemove ? markedRemoveColor : ColorForKind(e.data.kind);
            c.a = e.markedRemove ? 0.5f : 0.55f;
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
            _brushCursor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _brushCursor.name = "BrushCursor";
            _brushCursor.transform.SetParent(transform);
            var col = _brushCursor.GetComponent<Collider>();
            if (col != null) Destroy(col);
            _brushRenderer = _brushCursor.GetComponent<Renderer>();
            if (_brushRenderer != null)
            {
                _brushMaterial = CreateTransparentMaterial(brushCursorColor);
                _brushRenderer.sharedMaterial = _brushMaterial;
                _brushRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _brushRenderer.receiveShadows = false;
            }
        }

        private void UpdateBrushCursor(Vector2 mousePos, bool overUI)
        {
            if (_brushCursor == null) return;
            if (overUI || _mode == EditMode.None)
            {
                _brushCursor.SetActive(false);
                return;
            }
            if (!TryGetWorldUnderMouse(mousePos, out var w))
            {
                _brushCursor.SetActive(false);
                return;
            }
            _brushCursor.SetActive(true);
            float r = brushKm / GeoCoordinate.KmPerUnit;
            _brushCursor.transform.position = new Vector3(w.x, visualY + 0.5f, w.z);
            _brushCursor.transform.localScale = new Vector3(r * 2f, 0.05f, r * 2f);
            var c = _mode == EditMode.River ? riverColor : landColor;
            c.a = 0.5f;
            if (_brushMaterial != null)
            {
                if (_brushMaterial.HasProperty("_BaseColor")) _brushMaterial.SetColor("_BaseColor", c);
                if (_brushMaterial.HasProperty("_Color")) _brushMaterial.SetColor("_Color", c);
            }
        }

        // ─── UI 위 체크 (Canvas 만) ────────────────────────────────────────

        private static readonly List<RaycastResult> _uiRaycastResults = new();
        private static bool IsPointerOverCanvasUI(Vector2 mousePos)
        {
            if (EventSystem.current == null) return false;
            _uiRaycastResults.Clear();
            var ped = new PointerEventData(EventSystem.current) { position = mousePos };
            EventSystem.current.RaycastAll(ped, _uiRaycastResults);
            for (int i = 0; i < _uiRaycastResults.Count; i++)
            {
                // GraphicRaycaster = Canvas UI. 3D PhysicsRaycaster 는 무시.
                if (_uiRaycastResults[i].module is GraphicRaycaster) return true;
            }
            return false;
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
                // 항상 위에 그리기 — 메쉬에 가려져 안 보이는 문제 방지
                mat.renderQueue = 4000;
                if (mat.HasProperty("_ZTest"))
                    mat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
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
            // 폴리라인은 중심선을 실제 폭으로 한 줄로 그리기 (강 시각화)
            if (d.widthKm > 0f && d.points != null && d.points.Length >= 2)
            {
                lr.positionCount = d.points.Length;
                for (int i = 0; i < d.points.Length; i++)
                {
                    var w = GeoCoordinate.LatLngToWorld(d.points[i].y, d.points[i].x);
                    lr.SetPosition(i, MeshLocalXZToWorld(new Vector2(w.x, w.z)));
                }
                float widthUnits = d.widthKm / GeoCoordinate.KmPerUnit;
                lr.startWidth = widthUnits;
                lr.endWidth = widthUnits;
                lr.numCapVertices = 6;
                lr.numCornerVertices = 6;
                return;
            }

            // 폴리곤 (widthKm=0) — 외곽선 그리기
            var polys = MapSubtractGeometry.BuildSubtractPolygonsWorld(d);
            int total = 0;
            foreach (var p in polys) total += p.Length + 1;
            lr.positionCount = total;
            int cursor = 0;
            foreach (var poly in polys)
            {
                for (int i = 0; i < poly.Length; i++)
                    lr.SetPosition(cursor++, MeshLocalXZToWorld(poly[i]));
                lr.SetPosition(cursor++, MeshLocalXZToWorld(poly[0]));
            }
        }

        /// <summary>mesh-local XZ → world (Y=visualY). WorldLand transform 적용.</summary>
        private Vector3 MeshLocalXZToWorld(Vector2 meshLocalXZ)
        {
            var local = new Vector3(meshLocalXZ.x, visualY, meshLocalXZ.y);
            return _landTransform != null ? _landTransform.TransformPoint(local) : local;
        }

        // ─── 저장 / 취소 ───────────────────────────────────────────────────

        public void Save()
        {
#if UNITY_EDITOR
            EnsureFolder(saveFolder);

            int created = 0, removed = 0;

            // 1) 새 pending 폴리라인 → SO 생성
            foreach (var p in _pendings)
            {
                if (p.linePoints == null || p.linePoints.Count < 2) continue;
                var so = ScriptableObject.CreateInstance<MapSubtractData>();
                string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string uniq = $"{ts}_{created:D3}";
                so.subtractId = $"subtract.{p.kind.ToString().ToLower()}.{uniq}";
                so.displayNameKo = p.kind == MapEditKind.River ? $"강 {created + 1}" : $"땅 {created + 1}";
                so.kind = p.kind;
                so.widthKm = p.widthUnits * GeoCoordinate.KmPerUnit;   // unit → km
                so.enabled = true;
                so.points = LinePointsToLatLng(p.linePoints);
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

            // 7) 강 시각·통과 영역 즉시 갱신 — RiverOverlay 가 없으면 자동 생성
            var riverOverlay = FindAnyObjectByType<RiverOverlay>();
            if (riverOverlay == null)
            {
                var go = new GameObject("RiverOverlay (Auto)");
                riverOverlay = go.AddComponent<RiverOverlay>();
                riverOverlay.catalog = catalog;
                Debug.Log("[MapSubtractEditor] RiverOverlay 가 씬에 없어 자동 생성.");
            }
            else if (riverOverlay.catalog == null)
            {
                riverOverlay.catalog = catalog;
            }
            riverOverlay.Refresh();

            Debug.Log($"[MapSubtractEditor] 저장 완료. 새 영역 +{created}, 제거 -{removed}. " +
                (ok ? "메쉬 재베이크 완료." : "베이크 실패 — 메뉴 'Game ▸ Bake World Land' 수동 실행."));
#else
            Debug.LogWarning("[MapSubtractEditor] 저장은 Editor Play 모드에서만 가능.");
#endif
        }

        /// <summary>
        /// 카메라를 편집에 가장 적합한 화면 크기로 스냅 — top-down + 정해진 Y.
        /// 줌 레벨에 따라 클릭 위치가 어긋나는 문제를 한 방에 해결.
        /// 현재 XZ 는 유지 (보고 있던 위치 그대로).
        /// </summary>
        public void FitScreen()
        {
            if (mainCamera == null) return;
            var p = mainCamera.transform.position;
            p.y = fitScreenCameraY;
            mainCamera.transform.position = p;
            mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Debug.Log($"[MapSubtractEditor] 화면맞추기 — Y={fitScreenCameraY}, top-down 시점.");
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

        /// <summary>드래그로 그린 월드 좌표 점들 → MapSubtractData.points (lat/lng 배열).</summary>
        private Vector2[] LinePointsToLatLng(List<Vector3> worldPoints)
        {
            var arr = new Vector2[worldPoints.Count];
            for (int i = 0; i < worldPoints.Count; i++)
            {
                // WorldLand 가 (0,0,0) 외 위치면 mesh-local 로 변환해야 lat/lng 가 메쉬 정점과 일치
                Vector3 local = _landTransform != null
                    ? _landTransform.InverseTransformPoint(worldPoints[i])
                    : worldPoints[i];
                var ll = GeoCoordinate.WorldToLatLng(local);
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

            (_riverBtn, _riverBtnBg) = CreateBigButton(toolbar.transform, "강",
                () => SetMode(EditMode.River));
            (_landBtn, _landBtnBg) = CreateBigButton(toolbar.transform, "땅",
                () => SetMode(EditMode.Land));
            CreateBigButton(toolbar.transform, "화면맞추기", FitScreen,
                new Color(0.3f, 0.4f, 0.7f, 1f));
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
            int pendingRiver = 0, pendingLand = 0;
            foreach (var p in _pendings)
            {
                if (p.kind == MapEditKind.River) pendingRiver++;
                else pendingLand++;
            }
            int markedRemove = 0;
            foreach (var e in _existing) if (e.markedRemove) markedRemove++;
            int totalChanges = pendingRiver + pendingLand + markedRemove;

            string modeKo = _mode switch
            {
                EditMode.River  => "<color=#5599FF>강 그리기</color>",
                EditMode.Land => "<color=#FFB060>땅 칠하기</color>",
                _ => "<color=#888888>모드 없음 (지도 이동)</color>",
            };

            string saveHint = totalChanges > 0
                ? $"<color=#FFDD55><b>⚠ [저장] 버튼을 눌러야 실제 지도에 적용됩니다 ({totalChanges}개 대기 중)</b></color>"
                : "<color=#AAAAAA>우클릭=칠하기, Enter=모드 해제</color>";

            float camY = mainCamera != null ? mainCamera.transform.position.y : 0f;
            _statusText.text =
                $"<b>모드:</b> {modeKo}  |  <b>브러시:</b> {brushKm:F0} km  ([ ] 키)  |  " +
                $"<b>줌 Y:</b> {camY:F0}\n" +
                $"<b>변경:</b> 강 +{pendingRiver}, 땅 +{pendingLand}, 삭제 -{markedRemove}\n" +
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
