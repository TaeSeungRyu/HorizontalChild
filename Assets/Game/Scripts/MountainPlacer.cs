using System.Collections.Generic;
using UnityEngine;

// 지정한 자리에만 산/언덕을 배치합니다. (랜덤 아님 — 마커 둔 곳에만)
// 사용법:
//  1) 빈 GameObject 생성 → 이 스크립트 추가 (이름 예: MountainPlacer)
//  2) 그 아래에 빈 자식(마커)을 만들어 산 둘 자리로 이동
//     (Hierarchy에서 MountainPlacer 우클릭 → Create Empty → 씬에서 위치 이동. 여러 개 가능)
//  3) Mountain Prefabs에 산/언덕 프리팹 넣기 (여러 개면 무작위 선택)
//  4) 컴포넌트 우클릭(⋮) → "Place Mountains" (지우려면 "Clear")
//  ※ 지형(육지)에 Collider 필요. 마커 위치엔 노란 구가 표시됩니다.
public class MountainPlacer : MonoBehaviour
{
    [Header("산/언덕 프리팹 (여러 개면 무작위 선택)")]
    public GameObject[] mountainPrefabs;

    [Header("배치")]
    public bool snapToGround = true;     // 마커 X/Z에서 지면 높이를 찾아 앉힘
    public float raycastHeight = 1000f;
    public LayerMask groundMask = ~0;
    public float sinkInto = 0.5f;        // 바닥에 살짝 박아 틈 방지

    [Header("변화")]
    public Vector2 scaleRange = new Vector2(1f, 1.5f);
    public bool randomYaw = true;

    const string CONTAINER = "_PlacedMountains";

    [ContextMenu("Place Mountains")]
    public void Place()
    {
        if (mountainPrefabs == null || mountainPrefabs.Length == 0)
        { Debug.LogWarning("MountainPlacer: Mountain Prefabs가 비어 있어요."); return; }

        Clear();
        var container = new GameObject(CONTAINER);
        container.transform.SetParent(transform, false);

        var markers = new List<Transform>();
        foreach (Transform child in transform)
            if (child.name != CONTAINER) markers.Add(child);

        if (markers.Count == 0)
        { Debug.LogWarning("MountainPlacer: 마커(자식 Empty)가 없어요. 산 둘 자리에 Empty를 만들어 두세요."); return; }

        int placed = 0;
        foreach (var m in markers)
        {
            Vector3 pos = m.position;
            if (snapToGround)
            {
                Vector3 from = new Vector3(pos.x, pos.y + raycastHeight, pos.z);
                if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, raycastHeight * 4f, groundMask))
                    pos = hit.point;
            }
            pos.y -= sinkInto;

            var prefab = mountainPrefabs[Random.Range(0, mountainPrefabs.Length)];
            if (prefab == null) continue;
            var obj = Instantiate(prefab, pos, Quaternion.identity, container.transform);
            obj.transform.rotation = Quaternion.Euler(0f, randomYaw ? Random.Range(0f, 360f) : 0f, 0f);
            obj.transform.localScale = prefab.transform.localScale * Random.Range(scaleRange.x, scaleRange.y);
            placed++;
        }
        Debug.Log("MountainPlacer: " + placed + "개 배치 완료 (마커 " + markers.Count + "개).");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        var existing = transform.Find(CONTAINER);
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.45f, 0.2f);
        foreach (Transform child in transform)
            if (child.name != CONTAINER) Gizmos.DrawWireSphere(child.position, 1.5f);
    }
}
