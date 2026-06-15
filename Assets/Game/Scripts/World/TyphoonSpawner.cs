using System.Collections.Generic;
using Game.Ship;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 태풍 랜덤 생성기.
    ///
    /// 동작:
    ///   - 일정 간격으로 바다 위 랜덤 좌표에 Typhoon prefab 을 spawn.
    ///   - 유럽 영역은 spawn 제외 (어린이 시작 지역 보호).
    ///   - 육지 위는 자동 회피 (Physics.OverlapSphere 로 Landmass 검사).
    ///   - 플레이어 배가 태풍 hazard 반경 안에 들어오면:
    ///       1) 1초당 내구도 5% 감소 (소수점 누적)
    ///       2) 배가 흰색으로 하이라이트 (모든 Renderer)
    ///   - 태풍은 lifetime 후 자동 소멸.
    ///
    /// 개발 테스트:
    ///   - Inspector 의 [Spawn Now Test] ☑ → 즉시 1개 spawn (체크박스 자동 해제)
    /// </summary>
    public class TyphoonSpawner : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Typhoon 프리팹 (Typhoon.fbx 또는 그것을 prefab 화한 것).")]
        public GameObject typhoonPrefab;
        public ShipController playerShip;

        [Header("Spawning")]
        [Tooltip("동시 최대 활성 태풍 수.")]
        [Range(1, 10)] public int maxActive = 3;
        [Tooltip("자동 spawn 간격 (초).")]
        [Range(5f, 300f)] public float spawnIntervalSeconds = 45f;
        [Tooltip("태풍 한 개 수명 (초). 경과 시 자동 Destroy.")]
        [Range(10f, 600f)] public float lifetimeSeconds = 90f;
        [Tooltip("태풍 visual 스케일 (Typhoon.fbx 기본 크기 배수).")]
        [Range(0.5f, 5f)] public float typhoonScale = 1.0f;

        [Header("Hazard")]
        [Tooltip("배가 태풍 중심에서 이 거리(km) 안에 들어오면 피해. Typhoon 시각 크기랑 맞춰서.")]
        [Range(20f, 500f)] public float hazardRadiusKm = 120f;
        [Tooltip("1초당 배 내구도 감소율 (%). 5 = 1초에 5%.")]
        [Range(1f, 50f)] public float damagePercentPerSecond = 5f;
        public Color highlightColor = Color.white;

        [Header("Spawn Region — 유럽 제외 박스")]
        [Tooltip("유럽 박스 lat 범위 (이 안엔 spawn 안 함).")]
        public float europeLatMin = 35f;
        public float europeLatMax = 71f;
        [Tooltip("유럽 박스 lng 범위.")]
        public float europeLngMin = -10f;
        public float europeLngMax = 40f;
        [Tooltip("Spawn 가능한 전체 lat 범위 (남북극은 제외 권장).")]
        public float worldLatMin = -55f;
        public float worldLatMax = 65f;

        [Header("Test")]
        [Tooltip("☑ → 즉시 태풍 1개 생성 후 자동 해제. 개발 확인용.")]
        public bool spawnNowTest = false;
        [Tooltip("테스트 모드일 때 배 근처(이 거리만큼)에 spawn — 카메라가 따라가는 위치라 즉시 보임. 0=원래대로 랜덤.")]
        [Range(0f, 200f)] public float testSpawnNearShipUnits = 60f;

        // ─── 런타임 ────────────────────────────────────────────────────────
        private class Active
        {
            public GameObject go;
            public float expiresAt;
        }
        private readonly List<Active> _active = new();
        private float _nextSpawnAt;
        private readonly Collider[] _overlapBuf = new Collider[16];

        // 하이라이트
        private bool _highlighting;
        private float _damageBuffer;   // 누적 소수점 데미지
        private readonly List<Renderer> _shipRenderers = new();
        private readonly List<Material> _origMats = new();
        private readonly List<Material> _highlightMats = new();
        private GameObject _highlightShipRoot;

        private void Start()
        {
            _nextSpawnAt = Time.time + 5f;   // 시작 5초 후 첫 spawn
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>();
        }

        private void Update()
        {
            // 테스트 버튼 — 배 근처에 spawn 해서 즉시 확인 가능
            if (spawnNowTest)
            {
                spawnNowTest = false;
                SpawnOneNow(nearShipForTest: true);
            }

            // 자동 spawn (랜덤 위치)
            if (Time.time >= _nextSpawnAt && _active.Count < maxActive)
            {
                _nextSpawnAt = Time.time + spawnIntervalSeconds;
                SpawnOneNow(nearShipForTest: false);
            }

            // 수명 만료
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].go == null) { _active.RemoveAt(i); continue; }
                if (Time.time >= _active[i].expiresAt)
                {
                    Destroy(_active[i].go);
                    _active.RemoveAt(i);
                }
            }

            // Hazard 검사
            UpdateHazard();
        }

        // ─── Spawn ─────────────────────────────────────────────────────────

        public void SpawnOneNow(bool nearShipForTest = false)
        {
            if (typhoonPrefab == null)
            {
                Debug.LogWarning("[TyphoonSpawner] Typhoon Prefab 미할당. Inspector 에 할당해 주세요.");
                return;
            }

            Vector3 pos;
            if (nearShipForTest && playerShip != null && testSpawnNearShipUnits > 0f)
            {
                // 테스트 — 배 근처에 약간 떨어진 곳 (배 진행방향 앞쪽)
                var ship = playerShip.transform;
                pos = ship.position + ship.forward * testSpawnNearShipUnits;
                pos.y = 0f;
            }
            else
            {
                pos = PickRandomSeaPosition();
            }

            var go = Instantiate(typhoonPrefab, pos, Quaternion.identity);
            go.name = $"Typhoon_{_active.Count + 1}";
            if (typhoonScale != 1f) go.transform.localScale *= typhoonScale;
            // TyphoonMotion 이 없으면 자동 추가 (모델만 prefab 화한 경우 대비)
            if (go.GetComponent<TyphoonMotion>() == null) go.AddComponent<TyphoonMotion>();

            _active.Add(new Active { go = go, expiresAt = Time.time + lifetimeSeconds });

            // 진단 — Renderer 가 있는지 확인 (없으면 시각 안 나옴)
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            int rendererCount = renderers.Length;
            int activeRenderers = 0;
            foreach (var r in renderers)
            {
                if (r != null && r.enabled && r.gameObject.activeInHierarchy) activeRenderers++;
            }

            var (lat, lng) = GeoCoordinate.WorldToLatLng(pos);
            Debug.Log(
                $"[TyphoonSpawner] 태풍 spawn — '{go.name}' " +
                $"world={pos} (lat {lat:F1}°, lng {lng:F1}°), " +
                $"scale={go.transform.localScale}, Renderer 활성 {activeRenderers}/{rendererCount}, " +
                $"활성 태풍 {_active.Count}/{maxActive}");

            if (activeRenderers == 0)
            {
                Debug.LogError(
                    $"[TyphoonSpawner] Typhoon prefab '{typhoonPrefab.name}' 에 활성 Renderer 가 없습니다. " +
                    "FBX 를 Inspector 에서 확인 또는 prefab 으로 만들어 MeshRenderer 가 enabled 인지 확인해주세요.");
            }
        }

        private Vector3 PickRandomSeaPosition()
        {
            // 50번 시도 — 유럽 제외 + 육지 위 회피
            for (int attempt = 0; attempt < 50; attempt++)
            {
                float lat = Random.Range(worldLatMin, worldLatMax);
                float lng = Random.Range(-180f, 180f);

                // 유럽 박스 제외
                bool inEurope = lat >= europeLatMin && lat <= europeLatMax
                             && lng >= europeLngMin && lng <= europeLngMax;
                if (inEurope) continue;

                var pos = GeoCoordinate.LatLngToWorld(lat, lng);
                pos.y = 0f;

                // 육지 회피 — hazard 반경 안에 Landmass 가 있으면 reject
                float radius = hazardRadiusKm / GeoCoordinate.KmPerUnit;
                int count = Physics.OverlapSphereNonAlloc(pos, radius, _overlapBuf);
                bool nearLand = false;
                for (int i = 0; i < count; i++)
                {
                    if (_overlapBuf[i] == null) continue;
                    if (_overlapBuf[i].GetComponentInParent<Landmass>() != null)
                    {
                        nearLand = true; break;
                    }
                }
                if (nearLand) continue;

                return pos;
            }

            // Fallback — 대서양 한가운데
            Debug.LogWarning("[TyphoonSpawner] 50번 시도해도 적합한 바다 못 찾음 — 대서양 fallback.");
            return GeoCoordinate.LatLngToWorld(15f, -40f);
        }

        // ─── Hazard / 데미지 / 하이라이트 ──────────────────────────────────

        private void UpdateHazard()
        {
            if (playerShip == null) { ApplyHighlight(false); return; }

            float radius = hazardRadiusKm / GeoCoordinate.KmPerUnit;
            var shipPos = playerShip.transform.position;
            bool inHazard = false;
            foreach (var a in _active)
            {
                if (a.go == null) continue;
                float d = Vector3.Distance(shipPos, a.go.transform.position);
                if (d < radius) { inHazard = true; break; }
            }

            if (inHazard)
            {
                ApplyHighlight(true);
                // 1초당 MaxDurability * (damagePercentPerSecond / 100) 감소.
                // ApplyDamage 가 int 라 누적 → 1 이상이면 적용.
                float perSec = playerShip.MaxDurability * (damagePercentPerSecond / 100f);
                _damageBuffer += perSec * Time.deltaTime;
                if (_damageBuffer >= 1f)
                {
                    int dmg = Mathf.FloorToInt(_damageBuffer);
                    _damageBuffer -= dmg;
                    playerShip.ApplyDamage(dmg);
                }
            }
            else
            {
                ApplyHighlight(false);
                _damageBuffer = 0f;
            }
        }

        private void ApplyHighlight(bool on)
        {
            // playerShip 의 visual root 가 바뀌면 (RefreshVisual 호출 등) 캐시 재구축
            if (playerShip != null && playerShip.gameObject != _highlightShipRoot)
            {
                ClearHighlightCache();
                _highlightShipRoot = playerShip.gameObject;
            }

            if (_highlighting == on) return;
            _highlighting = on;

            // 캐시 lazy 구축
            if (_shipRenderers.Count == 0 && playerShip != null)
            {
                foreach (var r in playerShip.GetComponentsInChildren<Renderer>())
                {
                    if (r == null || r.sharedMaterial == null) continue;
                    _shipRenderers.Add(r);
                    _origMats.Add(r.sharedMaterial);
                    var hm = new Material(r.sharedMaterial);
                    if (hm.HasProperty("_BaseColor")) hm.SetColor("_BaseColor", highlightColor);
                    else if (hm.HasProperty("_Color")) hm.SetColor("_Color", highlightColor);
                    if (hm.HasProperty("_EmissionColor")) hm.SetColor("_EmissionColor", highlightColor * 0.5f);
                    _highlightMats.Add(hm);
                }
            }

            for (int i = 0; i < _shipRenderers.Count; i++)
            {
                if (_shipRenderers[i] == null) continue;
                _shipRenderers[i].sharedMaterial = on ? _highlightMats[i] : _origMats[i];
            }
        }

        private void ClearHighlightCache()
        {
            _shipRenderers.Clear();
            _origMats.Clear();
            foreach (var m in _highlightMats) if (m != null) Destroy(m);
            _highlightMats.Clear();
            _highlighting = false;
        }

        private void OnDisable()
        {
            ApplyHighlight(false);   // 게임 종료/씬 변경 시 원본 머티리얼 복원
        }

        private void OnDrawGizmosSelected()
        {
            // 유럽 제외 박스 시각화
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            var sw = GeoCoordinate.LatLngToWorld(europeLatMin, europeLngMin);
            var ne = GeoCoordinate.LatLngToWorld(europeLatMax, europeLngMax);
            var center = (sw + ne) * 0.5f;
            var size = new Vector3(ne.x - sw.x, 1f, ne.z - sw.z);
            Gizmos.DrawCube(new Vector3(center.x, 5f, center.z), size);
        }
    }
}
