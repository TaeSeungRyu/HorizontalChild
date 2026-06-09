using System.Collections.Generic;
using UnityEngine;

namespace Game.Ship
{
    /// <summary>
    /// 갤리선·갤리어스 등의 노(Oar)를 sin 곡선으로 흔들어 움직이는 느낌을 만들기.
    ///
    /// 사용법:
    ///   1) Blender 에서 노를 별도 메쉬로 분리, 각 노의 피벗(origin)을 노가 회전할 지점
    ///      (노받침 / rowlock) 에 둠.
    ///   2) 자식 GameObject 이름을 "Oar" 로 시작하게 (예: Oar_L_01, Oar_R_02).
    ///   3) 배 prefab root 에 본 컴포넌트 부착.
    ///   4) 인스펙터에서 swingAngle / swingSpeed 조정.
    ///
    /// 왼쪽·오른쪽 동기화:
    ///   - Oar_L_* 와 Oar_R_* 는 자동으로 같은 위상 (포트사이드 일제히 동기)
    ///   - 위상 분리 원하면 useAlternatingSides 켜기 (좌우가 180° 위상차로 교차)
    /// </summary>
    public class OarAnimator : MonoBehaviour
    {
        [Header("Swing")]
        [Tooltip("노가 양쪽으로 흔들리는 최대 각도 (도).")]
        [Range(5f, 60f)] public float swingAngle = 25f;

        [Tooltip("초당 사이클 수 (1.0 = 1초에 한 번 왕복).")]
        [Range(0.1f, 4f)] public float swingSpeed = 1.0f;

        [Tooltip("회전 축 (로컬). 기본 X = 노가 앞뒤로 흔들림.")]
        public Vector3 axis = Vector3.right;

        [Header("자동 검색")]
        [Tooltip("이 접두어로 시작하는 자식 GameObject 를 노로 인식. 콤마로 여러 개 가능 (예: 'Oar, 노').")]
        public string oarNamePrefixes = "Oar, 노, oar";

        [Tooltip("좌(Oar_L_*) 와 우(Oar_R_*) 가 180° 위상차로 교차 — 더 자연스러운 노젓기.")]
        public bool useAlternatingSides = true;

        [Header("수동 등록 (자동 검색 실패 시 직접 드래그)")]
        [Tooltip("Inspector 에서 직접 노 Transform 들을 드래그. 자동 검색 결과에 추가됨.")]
        public Transform[] manualOars;

        private struct OarEntry
        {
            public Transform tr;
            public Quaternion baseRotation;
            public float phaseOffset;   // 라디안
        }
        private readonly List<OarEntry> _oars = new();

        private void Start()
        {
            CollectOars(transform);

            // 수동 리스트 추가
            if (manualOars != null)
            {
                foreach (var m in manualOars)
                {
                    if (m == null) continue;
                    if (AlreadyTracking(m)) continue;
                    _oars.Add(new OarEntry
                    {
                        tr = m,
                        baseRotation = m.localRotation,
                        phaseOffset = ComputePhase(m.name),
                    });
                }
            }

            if (_oars.Count == 0)
            {
                Debug.LogWarning(
                    $"[OarAnimator:{name}] 노 0 개 — 자식 이름이 '{oarNamePrefixes}' 중 어느 것으로도 시작 안 함. " +
                    "우클릭 → 'Log All Children' 으로 실제 이름 확인하거나, 인스펙터의 Manual Oars 에 직접 드래그하세요.");
            }
            else
            {
                Debug.Log($"[OarAnimator:{name}] 노 {_oars.Count} 개 인식.");
            }
        }

        private bool AlreadyTracking(Transform t)
        {
            for (int i = 0; i < _oars.Count; i++) if (_oars[i].tr == t) return true;
            return false;
        }

        private void CollectOars(Transform parent)
        {
            var prefixList = ParsePrefixes();
            CollectOarsRecursive(parent, prefixList);
        }

        private void CollectOarsRecursive(Transform parent, string[] prefixes)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (MatchesAnyPrefix(child.name, prefixes))
                {
                    _oars.Add(new OarEntry
                    {
                        tr = child,
                        baseRotation = child.localRotation,
                        phaseOffset = ComputePhase(child.name),
                    });
                }
                CollectOarsRecursive(child, prefixes);
            }
        }

        private string[] ParsePrefixes()
        {
            if (string.IsNullOrWhiteSpace(oarNamePrefixes)) return new[] { "Oar" };
            var parts = oarNamePrefixes.Split(',');
            var result = new List<string>();
            foreach (var p in parts)
            {
                var trimmed = p.Trim();
                if (!string.IsNullOrEmpty(trimmed)) result.Add(trimmed);
            }
            return result.ToArray();
        }

        private bool MatchesAnyPrefix(string childName, string[] prefixes)
        {
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (childName.StartsWith(prefixes[i], System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private float ComputePhase(string childName)
        {
            if (!useAlternatingSides) return 0f;
            // "Oar_L_*" → 0, "Oar_R_*" → π. 패턴이 다르면 0.
            var upper = childName.ToUpperInvariant();
            if (upper.Contains("_R_") || upper.EndsWith("_R") || upper.Contains("RIGHT")) return Mathf.PI;
            return 0f;
        }

        private void Update()
        {
            if (_oars.Count == 0) return;
            float t = Time.time * swingSpeed * Mathf.PI * 2f;
            for (int i = 0; i < _oars.Count; i++)
            {
                var e = _oars[i];
                if (e.tr == null) continue;
                float angle = Mathf.Sin(t + e.phaseOffset) * swingAngle;
                e.tr.localRotation = e.baseRotation * Quaternion.AngleAxis(angle, axis);
            }
        }

        // ─── 디버그 ────────────────────────────────────────────────────────

        [ContextMenu("Find Oars Now")]
        private void DebugFindOars()
        {
            _oars.Clear();
            CollectOars(transform);
            Debug.Log($"[OarAnimator:{name}] 노 {_oars.Count} 개 발견.");
        }

        [ContextMenu("Log All Children")]
        private void LogAllChildren()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[OarAnimator:{name}] 모든 자식 GameObject:");
            LogChildrenRecursive(transform, sb, 0);
            Debug.Log(sb.ToString());
        }

        private void LogChildrenRecursive(Transform parent, System.Text.StringBuilder sb, int depth)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                sb.Append(' ', depth * 2);
                sb.Append("- ").AppendLine(child.name);
                LogChildrenRecursive(child, sb, depth + 1);
            }
        }
    }
}
