using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Data;

// 강/물길 페인터 — 2D 세계지도 위에 경로를 클릭해 그리면 MapSubtractData 로 저장.
// 그 뒤 'Game ▸ Bake World Land Mesh from GeoJSON' 실행하면 반영.
// 메뉴: Game ▸ River Painter
//
// 모드:  Draw = 좌클릭으로 점 추가 / Erase = 기존 강 선 클릭 시 삭제
// 줌·이동: 마우스휠 = 커서 기준 확대/축소, 중간(휠)버튼 드래그 = 지도 이동, [Reset View]
//   확대하면 지도와 강 마킹이 함께 커지고, 클릭 위치도 정확히 보정됩니다.
public class RiverPaintWindow : EditorWindow
{
    enum Mode { Draw, Erase }

    const string PreviewPath = "Assets/Game/Art/Map/MapPreview.png";
    const string SaveFolder  = "Assets/Game/Data/MapSubtracts";

    Texture2D _map;
    readonly List<Vector2> _pts = new List<Vector2>();   // (lng,lat)
    float _widthKm = 200f;
    MapEditKind _kind = MapEditKind.Sea;
    string _riverName = "river_1";
    Mode _mode = Mode.Draw;
    bool _showExisting = true;
    MapSubtractData _editing;
    List<MapSubtractData> _existing = new List<MapSubtractData>();
    Vector2 _listScroll;

    // ── 줌/이동 상태 ──
    float _zoom = 1f;                 // 1 = 화면에 딱 맞음
    Vector2 _pan = Vector2.zero;      // 추가 이동(픽셀)
    const float MinZoom = 1f, MaxZoom = 12f;

    [MenuItem("Game/River Painter")]
    static void Open()
    {
        var w = GetWindow<RiverPaintWindow>("River Painter");
        w.minSize = new Vector2(720, 560);
        w.LoadMap(); w.RefreshExisting();
    }

    void LoadMap() => _map = AssetDatabase.LoadAssetAtPath<Texture2D>(PreviewPath);
    void RefreshExisting()
    {
        _existing.Clear();
        foreach (var g in AssetDatabase.FindAssets("t:MapSubtractData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<MapSubtractData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null) _existing.Add(d);
        }
    }

    // 지도가 그려지는 기준 사각형(줌·이동 적용 전, 창에 꽉 채운 2:1)
    Rect _baseRect;
    // 줌·이동을 적용한 실제 지도 사각형
    Rect MapRect()
    {
        float w = _baseRect.width * _zoom;
        float h = _baseRect.height * _zoom;
        // 줌 중심을 baseRect 중심으로 두고 _pan 만큼 이동
        float cx = _baseRect.center.x + _pan.x;
        float cy = _baseRect.center.y + _pan.y;
        return new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
    }

    void OnGUI()
    {
        if (_map == null) LoadMap();

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            _mode = (Mode)EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarPopup, GUILayout.Width(70));
            using (new EditorGUI.DisabledScope(_mode != Mode.Draw))
            {
                _kind = (MapEditKind)EditorGUILayout.EnumPopup(_kind, EditorStyles.toolbarPopup, GUILayout.Width(80));
                GUILayout.Label("Width(km)", GUILayout.Width(58));
                _widthKm = EditorGUILayout.Slider(_widthKm, 10f, 500f, GUILayout.Width(140));
                GUILayout.Label("이름", GUILayout.Width(26));
                _riverName = EditorGUILayout.TextField(_riverName, GUILayout.Width(100));
                if (GUILayout.Button("Undo Point", EditorStyles.toolbarButton) && _pts.Count > 0)
                    _pts.RemoveAt(_pts.Count - 1);
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton)) { _pts.Clear(); _editing = null; }
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_zoom:F1}x", GUILayout.Width(34));
            if (GUILayout.Button("－", EditorStyles.toolbarButton, GUILayout.Width(24))) ZoomAt(_baseRect.center, 1f/1.3f);
            if (GUILayout.Button("＋", EditorStyles.toolbarButton, GUILayout.Width(24))) ZoomAt(_baseRect.center, 1.3f);
            if (GUILayout.Button("Reset View", EditorStyles.toolbarButton)) { _zoom = 1f; _pan = Vector2.zero; }
            _showExisting = GUILayout.Toggle(_showExisting, "기존 표시", EditorStyles.toolbarButton);
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton)) { LoadMap(); RefreshExisting(); }
            using (new EditorGUI.DisabledScope(_pts.Count < 2))
                if (GUILayout.Button(_editing != null ? "● Save(덮어쓰기)" : "● Save River", EditorStyles.toolbarButton))
                    SaveRiver();
        }

        EditorGUILayout.HelpBox(
            (_mode == Mode.Draw
              ? "Draw: 좌클릭으로 강 경로(중심선). Kind=Sea: 배 물리 통과 / River: 통과 허용."
              : "Erase: 기존 강 선을 클릭하면 삭제.")
            + "  휠=확대/축소, 휠버튼 드래그=이동, Reset View=원위치. 저장 후 Bake 실행.",
            MessageType.Info);

        float listH = 150f;
        Rect area = GUILayoutUtility.GetRect(position.width, position.height - 96 - listH);
        // baseRect: area 안에 2:1 꽉 채움
        float cw = area.width, ch = cw * 0.5f;
        if (ch > area.height) { ch = area.height; cw = ch * 2f; }
        _baseRect = new Rect(area.x + (area.width - cw) * 0.5f, area.y, cw, ch);
        Rect map = MapRect();

        // 클리핑: area 밖으로 안 넘치게
        GUI.BeginClip(area);
        Rect mapLocal = new Rect(map.x - area.x, map.y - area.y, map.width, map.height);
        if (_map != null) GUI.DrawTexture(mapLocal, _map, ScaleMode.StretchToFill);
        else EditorGUI.DrawRect(mapLocal, new Color(0.1f, 0.2f, 0.3f));

        Handles.BeginGUI();
        Vector2 off = new Vector2(area.x, area.y);
        if (_showExisting)
            foreach (var d in _existing)
            {
                if (d == null || d.points == null || d.points.Length < 2) continue;
                Handles.color = d.enabled ? new Color(0.4f, 0.8f, 1f, 0.55f) : new Color(0.6f, 0.6f, 0.6f, 0.35f);
                var prev = LngLatToMap(d.points[0], map) - off;
                for (int i = 1; i < d.points.Length; i++)
                { var c = LngLatToMap(d.points[i], map) - off; Handles.DrawAAPolyLine(3f, prev, c); prev = c; }
            }
        if (_pts.Count > 0)
        {
            Handles.color = (_kind == MapEditKind.Sea) ? new Color(0.2f, 0.9f, 1f) : new Color(0.3f, 1f, 0.5f);
            Vector2 prev = LngLatToMap(_pts[0], map) - off; DrawDot(prev);
            for (int i = 1; i < _pts.Count; i++)
            { Vector2 c = LngLatToMap(_pts[i], map) - off; Handles.DrawAAPolyLine(5f, prev, c); DrawDot(c); prev = c; }
        }
        Handles.EndGUI();
        GUI.EndClip();

        HandleInput(area, map);

        if (area.Contains(Event.current.mousePosition))
        {
            var ll = MapToLngLat(Event.current.mousePosition, map);
            EditorGUI.LabelField(new Rect(area.x + 4, area.yMax - 18, 320, 16),
                $"경도 {ll.x:F2}°  위도 {ll.y:F2}°   (점 {_pts.Count})  [{_mode}] {_zoom:F1}x",
                EditorStyles.whiteMiniLabel);
            Repaint();
        }

        GUILayout.Space(4);
        EditorGUILayout.LabelField($"저장된 영역 ({_existing.Count})", EditorStyles.boldLabel);
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(listH - 24));
        for (int i = 0; i < _existing.Count; i++)
        {
            var d = _existing[i]; if (d == null) continue;
            using (new EditorGUILayout.HorizontalScope())
            {
                bool en = EditorGUILayout.Toggle(d.enabled, GUILayout.Width(18));
                if (en != d.enabled) { Undo.RecordObject(d, "toggle"); d.enabled = en; EditorUtility.SetDirty(d); }
                EditorGUILayout.LabelField($"{d.displayNameKo}  ({d.kind}, {d.widthKm:F0}km, {(d.points!=null?d.points.Length:0)}점)");
                if (GUILayout.Button("Edit", GUILayout.Width(50))) LoadForEdit(d);
                if (GUILayout.Button("Delete", GUILayout.Width(60))) DeleteRiver(d);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void HandleInput(Rect area, Rect map)
    {
        Event e = Event.current;
        if (!area.Contains(e.mousePosition)) return;

        // 휠 = 커서 기준 확대/축소
        if (e.type == EventType.ScrollWheel)
        {
            float factor = e.delta.y < 0 ? 1.15f : 1f / 1.15f;
            ZoomAt(e.mousePosition, factor);
            e.Use(); Repaint(); return;
        }
        // 중간(휠) 버튼 드래그 = 이동
        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            _pan += e.delta; e.Use(); Repaint(); return;
        }
        // 좌클릭 = 그리기/지우개
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (_mode == Mode.Draw) _pts.Add(MapToLngLat(e.mousePosition, map));
            else EraseAt(e.mousePosition, map);
            e.Use(); Repaint();
        }
    }

    // 커서 위치(피벗)를 유지하면서 줌
    void ZoomAt(Vector2 pivot, float factor)
    {
        float newZoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
        if (Mathf.Approximately(newZoom, _zoom)) return;
        Rect before = MapRect();
        // 피벗의 지도 내 비율
        float u = (pivot.x - before.x) / before.width;
        float v = (pivot.y - before.y) / before.height;
        _zoom = newZoom;
        // 줌 후 같은 비율 지점이 같은 화면 위치에 오도록 pan 보정
        float w = _baseRect.width * _zoom, h = _baseRect.height * _zoom;
        float cx = _baseRect.center.x, cy = _baseRect.center.y;
        // 새 map (pan 제외) 기준에서 피벗 지점 위치
        float nx = (cx - w * 0.5f) + u * w;
        float ny = (cy - h * 0.5f) + v * h;
        _pan = new Vector2(pivot.x - nx, pivot.y - ny);
    }

    void DrawDot(Vector2 p) { var c = Handles.color; Handles.DrawSolidDisc(p, Vector3.forward, 4f); Handles.color = c; }

    // (lng,lat) → 화면 픽셀 (현재 map 사각형 기준)
    Vector2 LngLatToMap(Vector2 ll, Rect m)
        => new Vector2(m.x + (ll.x + 180f) / 360f * m.width, m.y + (90f - ll.y) / 180f * m.height);
    // 화면 픽셀 → (lng,lat)
    Vector2 MapToLngLat(Vector2 p, Rect m)
        => new Vector2(-180f + Mathf.Clamp01((p.x - m.x) / m.width) * 360f, 90f - Mathf.Clamp01((p.y - m.y) / m.height) * 180f);

    void EraseAt(Vector2 mouse, Rect map)
    {
        MapSubtractData best = null; float bestD = 10f;
        foreach (var d in _existing)
        {
            if (d == null || d.points == null || d.points.Length < 2) continue;
            for (int i = 0; i < d.points.Length - 1; i++)
            {
                Vector2 a = LngLatToMap(d.points[i], map), b = LngLatToMap(d.points[i + 1], map);
                float dist = HandleUtility.DistancePointToLineSegment(mouse, a, b);
                if (dist < bestD) { bestD = dist; best = d; }
            }
        }
        if (best != null) DeleteRiver(best);
    }

    void LoadForEdit(MapSubtractData d)
    {
        _pts.Clear();
        if (d.points != null) _pts.AddRange(d.points);
        _kind = d.kind; _widthKm = d.widthKm; _riverName = d.displayNameKo; _editing = d; _mode = Mode.Draw;
        Repaint();
    }

    void DeleteRiver(MapSubtractData d)
    {
        if (!EditorUtility.DisplayDialog("삭제", $"'{d.displayNameKo}' 를 삭제할까요?\n(Bake 다시 실행해야 반영)", "삭제", "취소")) return;
        string path = AssetDatabase.GetAssetPath(d);
        if (_editing == d) { _editing = null; _pts.Clear(); }
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        RefreshExisting(); Repaint();
    }

    void SaveRiver()
    {
        if (!Directory.Exists(SaveFolder)) Directory.CreateDirectory(SaveFolder);
        MapSubtractData data = _editing != null ? _editing : ScriptableObject.CreateInstance<MapSubtractData>();
        data.subtractId = _riverName; data.displayNameKo = _riverName;
        data.kind = _kind; data.widthKm = _widthKm; data.points = _pts.ToArray(); data.enabled = true;
        string path;
        if (_editing != null) { EditorUtility.SetDirty(data); path = AssetDatabase.GetAssetPath(data); }
        else { path = AssetDatabase.GenerateUniqueAssetPath($"{SaveFolder}/MapSubtract_{_riverName}.asset"); AssetDatabase.CreateAsset(data, path); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        _editing = data; RefreshExisting();
        EditorUtility.DisplayDialog("River Painter",
            $"저장됨:\n{path}\n\n'Game ▸ Bake World Land Mesh from GeoJSON' 실행하면 반영됩니다.", "OK");
    }
}
