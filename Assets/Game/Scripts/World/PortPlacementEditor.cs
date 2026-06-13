using System.Collections.Generic;
using Game.Data;
using Game.Ship;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.World
{
    /// <summary>
    /// 런타임 위치 에디터 — 항구·발견물 핸들을 보여주고 드래그로 옮긴 뒤 SO 에셋에 직접 저장.
    ///
    /// 사용:
    ///   1) 씬에 빈 GameObject 만들고 본 컴포넌트 부착.
    ///   2) Inspector 에서 PortCatalog, DiscoveryCatalog 할당.
    ///   3) enableOnStart = true 면 Play 시 자동 활성 (Toggle 메뉴 / 버튼으로 끄기 가능).
    ///
    /// 동작:
    ///   - 모든 항구 + 발견물에 클릭 가능한 구 핸들 생성. 각자 이름 라벨.
    ///   - 핸들 클릭 + 드래그 → 위치 이동.
    ///   - 핸들 놓는 순간 PortData.latitude/longitude 또는 DiscoveryData.latitude/longitude 갱신
    ///     + AssetDatabase.SaveAssets (에디터 빌드에서만, UNITY_EDITOR 가드).
    ///   - 빈 화면 우클릭 드래그 → 카메라 팬.
    ///   - 마우스 휠 → 카메라 높이 변경 (선택).
    ///
    /// 빌드(.exe/.apk) 에선 저장 부분이 동작 안 함 — 에디터 Play 모드 전용 툴.
    /// </summary>
    public class PortPlacementEditor : MonoBehaviour
    {
        [Header("Catalogs")]
        public PortCatalog portCatalog;
        public DiscoveryCatalog discoveryCatalog;

        [Header("Camera")]
        public Camera mainCamera;
        [Tooltip("우클릭 드래그 카메라 팬 속도 (월드 unit / 화면 pixel).")]
        public float panSpeed = 0.5f;
        [Tooltip("마우스 휠 카메라 줌 속도.")]
        public float zoomSpeed = 50f;
        [Tooltip("카메라 줌 최소·최대 Y.")]
        public float minCameraY = 50f;
        public float maxCameraY = 2000f;

        [Header("Handle Visual")]
        [Tooltip("핸들 구의 크기.")]
        public float handleSize = 10f;
        [Tooltip("핸들의 Y 위치 (월드).")]
        public float handleY = 5f;
        public Color portColor = new Color(0.3f, 0.7f, 1f);
        public Color discoveryColor = new Color(1f, 0.8f, 0.3f);
        public Color selectedColor = new Color(0.4f, 1f, 0.4f);

        [Header("Label")]
        public TMP_FontAsset labelFont;
        public float labelFontSize = 6f;
        public float labelYOffset = 1.6f;   // 핸들 위쪽 offset (loc unit)

        [Header("Behavior")]
        [Tooltip("Play 시 자동으로 에디터 모드 활성.")]
        public bool enableOnStart = false;
        [Tooltip("에디터 모드 중 플레이어 배 입력 잠금.")]
        public ShipController playerShip;

        // 런타임
        private bool _active;
        private readonly List<GameObject> _handles = new();
        private EditorHandle _selectedHandle;
        private Vector3 _camDragLastMousePos;
        private bool _camDragging;

        public bool IsActive => _active;

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (enableOnStart) Enable();
        }

        [ContextMenu("Enable Editor Mode")]
        public void Enable()
        {
            if (_active) return;
            _active = true;
            if (playerShip != null) playerShip.LockInput = true;
            SeaSimulation.Pause(this);   // NPC 이동 정지
            BuildHandles();
            Debug.Log("[PortPlacementEditor] 에디터 모드 ON — 핸들 클릭·드래그로 이동, 우클릭 드래그로 팬, 휠로 줌.");
        }

        [ContextMenu("Disable Editor Mode")]
        public void Disable()
        {
            if (!_active) return;
            _active = false;
            if (playerShip != null) playerShip.LockInput = false;
            SeaSimulation.Resume(this);
            DestroyHandles();
            Debug.Log("[PortPlacementEditor] 에디터 모드 OFF.");
        }

        public void Toggle()
        {
            if (_active) Disable(); else Enable();
        }

        // ─── 핸들 빌드 ──────────────────────────────────────────────────────

        private void BuildHandles()
        {
            DestroyHandles();
            if (portCatalog != null && portCatalog.all != null)
            {
                foreach (var p in portCatalog.all)
                {
                    if (p == null) continue;
                    _handles.Add(CreateHandle(p.displayNameKo, p.portId,
                        GeoCoordinate.LatLngToWorld(p.latitude, p.longitude),
                        portColor, port: p, discovery: null));
                }
            }
            if (discoveryCatalog != null && discoveryCatalog.all != null)
            {
                foreach (var d in discoveryCatalog.all)
                {
                    if (d == null) continue;
                    _handles.Add(CreateHandle(d.displayNameKo, d.discoveryId,
                        GeoCoordinate.LatLngToWorld(d.latitude, d.longitude),
                        discoveryColor, port: null, discovery: d));
                }
            }
            Debug.Log($"[PortPlacementEditor] 핸들 {_handles.Count} 개 생성 ({portCatalog?.all?.Length ?? 0} 항구 + {discoveryCatalog?.all?.Length ?? 0} 발견물)");
        }

        private void DestroyHandles()
        {
            foreach (var go in _handles)
            {
                if (go != null) Destroy(go);
            }
            _handles.Clear();
            _selectedHandle = null;
        }

        private GameObject CreateHandle(string label, string id, Vector3 worldPos, Color color,
            PortData port, DiscoveryData discovery)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Handle_{id}";
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(worldPos.x, handleY, worldPos.z);
            go.transform.localScale = Vector3.one * handleSize;

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                rend.material = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // SphereCollider 기본 부착 — IPointerDownHandler 동작용

            var handle = go.AddComponent<EditorHandle>();
            handle.Init(this, port, discovery);

            // 이름 라벨
            CreateLabel(go, label);

            return go;
        }

        private void CreateLabel(GameObject parent, string text)
        {
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(parent.transform, false);
            labelGO.transform.localPosition = new Vector3(0f, labelYOffset, 0f);
            // 핸들 scale 이 handleSize 라 라벨이 휘말림 → localScale 보정
            labelGO.transform.localScale = Vector3.one / handleSize;

            var tmp = labelGO.AddComponent<TextMeshPro>();
            if (labelFont != null) tmp.font = labelFont;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = labelFontSize;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;
            tmp.faceColor = Color.white;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            tmp.richText = true;

            // RectTransform 크기 충분히
            var rt = labelGO.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(40f, 8f);
        }

        // ─── 카메라 팬·줌 ────────────────────────────────────────────────────

        private void Update()
        {
            if (!_active || mainCamera == null) return;

            // 우클릭 드래그 카메라 팬
            if (Input.GetMouseButtonDown(1))
            {
                _camDragging = true;
                _camDragLastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1))
            {
                _camDragging = false;
            }
            if (_camDragging)
            {
                Vector3 mouseDelta = Input.mousePosition - _camDragLastMousePos;
                _camDragLastMousePos = Input.mousePosition;
                // 화면 픽셀 → 월드 unit (Y 높이에 비례하게 살짝 키움)
                float scaleFactor = panSpeed * Mathf.Max(1f, mainCamera.transform.position.y / 100f);
                var worldDelta = new Vector3(-mouseDelta.x * scaleFactor, 0f, -mouseDelta.y * scaleFactor);
                mainCamera.transform.position += worldDelta;
            }

            // 마우스 휠 줌 (Y 축 변경)
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.001f)
            {
                var camPos = mainCamera.transform.position;
                camPos.y = Mathf.Clamp(camPos.y - wheel * zoomSpeed, minCameraY, maxCameraY);
                mainCamera.transform.position = camPos;
            }
        }

        // ─── 선택 표시 ──────────────────────────────────────────────────────

        public void NotifyHandleSelected(EditorHandle handle)
        {
            // 이전 선택 색 복원
            if (_selectedHandle != null && _selectedHandle != handle)
            {
                ResetHandleColor(_selectedHandle.gameObject);
            }
            _selectedHandle = handle;
            // 새 선택을 초록색으로
            var rend = handle.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
            {
                if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", selectedColor);
                else if (rend.material.HasProperty("_Color")) rend.material.SetColor("_Color", selectedColor);
            }
        }

        private void ResetHandleColor(GameObject handleGO)
        {
            var rend = handleGO.GetComponent<Renderer>();
            if (rend == null || rend.material == null) return;
            // 핸들 이름으로 port/discovery 구분 안 됨 → EditorHandle 의 _port/_discovery 보면 되지만 private
            // 간단히: GameObject 의 이름이 "Handle_port." 로 시작하면 port, 그 외 discovery
            Color c = handleGO.name.StartsWith("Handle_port.") ? portColor : discoveryColor;
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", c);
            else if (rend.material.HasProperty("_Color")) rend.material.SetColor("_Color", c);
        }

        // ─── 저장 ───────────────────────────────────────────────────────────

        public void SavePortPosition(PortData port, Vector3 newWorldPos)
        {
            if (port == null) return;
            var (lat, lng) = GeoCoordinate.WorldToLatLng(newWorldPos);
            port.latitude = lat;
            port.longitude = lng;
#if UNITY_EDITOR
            EditorUtility.SetDirty(port);
            AssetDatabase.SaveAssets();
#endif
            Debug.Log($"[PortPlacementEditor] {port.displayNameKo} → lat {lat:F2}, lng {lng:F2} 저장.");
        }

        public void SaveDiscoveryPosition(DiscoveryData disc, Vector3 newWorldPos)
        {
            if (disc == null) return;
            var (lat, lng) = GeoCoordinate.WorldToLatLng(newWorldPos);
            disc.latitude = lat;
            disc.longitude = lng;
#if UNITY_EDITOR
            EditorUtility.SetDirty(disc);
            AssetDatabase.SaveAssets();
#endif
            Debug.Log($"[PortPlacementEditor] {disc.displayNameKo} → lat {lat:F2}, lng {lng:F2} 저장.");
        }
    }
}
