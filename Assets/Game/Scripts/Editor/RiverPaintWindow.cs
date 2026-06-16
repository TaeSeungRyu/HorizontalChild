using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Data;

// 강/물길 페인터 — 2D 세계지도 위에 경로를 클릭해 그리면 MapSubtractData 로 저장.
// 그 뒤 'Game ▸ Bake World Land Mesh from GeoJSON' 실행하면 반영.
// 메뉴: Game ▸ River Painter
//
// 모드:
//   Draw  : 좌클릭으로 강 경로 점 추가
//   Erase : 기존 강 선을 클릭하면 그 강 삭제
// 그리는 중: Undo Point / Clear
// 하단 목록: 켜기·끄기 / Edit(불러와 수정) / Delete
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
    MapSubtractData _editing;            // Edit 로 불러온 원본(있으면 Save 시 덮어씀)
    List<MapSubtractData> _existing = new List<MapSubtractData>();
    Vector2 _listScroll;

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

    void OnGUI()
    {
        if (_map == null) LoadMap();

        // ── 툴바 ──
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            _mode = (Mode)EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarPopup, GUILayout.Width(70));
            using (new EditorGUI.DisabledScope(_mode != Mode.Draw))
            {
                _kind = (MapEditKind)EditorGUILayout.EnumPopup(_kind, EditorStyles.toolbarPopup, GUILayout.Width(80));
                GUILayout.Label("Width(km)", GUILayout.Width(58));
                _widthKm = EditorGUILayout.Slider(_widthKm, 10f, 500f, GUILayout.Width(150));
                GUILayout.Label("이름", GUILayout.Width(26));
                _riverName = EditorGUILayout.TextField(_riverName, GUILayout.Width(110));
                if (GUILayout.Button("Undo Point", EditorStyles.toolbarButton) && _pts.Count > 0)
                    _pts.RemoveAt(_pts.Count - 1);
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton)) { _pts.Clear(); _editing = null; }
            }
            GUILayout.FlexibleSpace();
            _showExisting = GUILayout.Toggle(_showExisting, "기존 표시", EditorStyles.toolbarButton);
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton)) { LoadMap(); RefreshExisting(); }
            using (new EditorGUI.DisabledScope(_pts.Count < 2))
                if (GUILayout.Button(_editing != null ? "● Save(덮어쓰기)" : "● Save River", EditorStyles.toolbarButton))
                    SaveRiver();
        }

        EditorGUILayout.HelpBox(
            _mode == Mode.Draw
              ? "Draw: 지도 위 좌클릭으로 강 경로(중심선)를 그리세요. 점 2개 이상이면 저장.\nKind=Sea: 진짜 물길(배 물리 통과) / River: 육지 두고 통과. 저장 후 Bake 실행."
              : "Erase: 지도 위 기존 강 선을 클릭하면 그 강이 삭제됩니다.",
            MessageType.Info);

        // ── 캔버스 (2:1) ──
        float listH = 150f;
        Rect area = GUILayoutUtility.GetRect(position.width, position.height - 96 - listH);
        float cw = area.width, ch = cw * 0.5f;
        if (ch > area.height) { ch = area.height; cw = ch * 2f; }
        Rect canvas = new Rect(area.x + (area.width - cw) * 0.5f, area.y, cw, ch);

        if (_map != null) GUI.DrawTexture(canvas, _map, ScaleMode.StretchToFill);
        else EditorGUI.DrawRect(canvas, new Color(0.1f, 0.2f, 0.3f));

        Handles.BeginGUI();
        if (_showExisting)
            foreach (var d in _existing)
            {
                if (d == null || d.points == null || d.points.Length < 2) continue;
                Handles.color = d.enabled ? new Color(0.4f, 0.8f, 1f, 0.55f) : new Color(0.6f, 0.6f, 0.6f, 0.35f);
                var prev = LngLatToCanvas(d.points[0], canvas);
                for (int i = 1; i < d.points.Length; i++)
                { var c = LngLatToCanvas(d.points[i], canvas); Handles.DrawAAPolyLine(3f, prev, c); prev = c; }
            }
        if (_pts.Count > 0)
        {
            Handles.color = (_kind == MapEditKind.Sea) ? new Color(0.2f, 0.9f, 1f) : new Color(0.3f, 1f, 0.5f);
            Vector2 prev = LngLatToCanvas(_pts[0], canvas); DrawDot(prev);
            for (int i = 1; i < _pts.Count; i++)
            { Vector2 c = LngLatToCanvas(_pts[i], canvas); Handles.DrawAAPolyLine(5f, prev, c); DrawDot(c); prev = c; }
        }
        Handles.EndGUI();

        // ── 마우스 ──
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && canvas.Contains(e.mousePosition))
        {
            if (_mode == Mode.Draw) { _pts.Add(CanvasToLngLat(e.mousePosition, canvas)); }
            else EraseAt(e.mousePosition, canvas);
            e.Use(); Repaint();
        }
        if (canvas.Contains(e.mousePosition))
        {
            var ll = CanvasToLngLat(e.mousePosition, canvas);
            EditorGUI.LabelField(new Rect(canvas.x + 4, canvas.yMax - 18, 300, 16),
                $"경도 {ll.x:F1}°  위도 {ll.y:F1}°   (점 {_pts.Count})  [{_mode}]", EditorStyles.whiteMiniLabel);
            Repaint();
        }

        // ── 하단: 기존 강 목록 ──
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

    void DrawDot(Vector2 p) { var c = Handles.color; Handles.DrawSolidDisc(p, Vector3.forward, 4f); Handles.color = c; }

    Vector2 LngLatToCanvas(Vector2 ll, Rect c)
        => new Vector2(c.x + (ll.x + 180f) / 360f * c.width, c.y + (90f - ll.y) / 180f * c.height);
    Vector2 CanvasToLngLat(Vector2 m, Rect c)
        => new Vector2(-180f + Mathf.Clamp01((m.x - c.x) / c.width) * 360f, 90f - Mathf.Clamp01((m.y - c.y) / c.height) * 180f);

    void EraseAt(Vector2 mouse, Rect canvas)
    {
        MapSubtractData best = null; float bestD = 10f; // 픽셀 임계
        foreach (var d in _existing)
        {
            if (d == null || d.points == null || d.points.Length < 2) continue;
            for (int i = 0; i < d.points.Length - 1; i++)
            {
                Vector2 a = LngLatToCanvas(d.points[i], canvas), b = LngLatToCanvas(d.points[i + 1], canvas);
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
        if (!EditorUtility.DisplayDialog("삭제", $"'{d.displayNameKo}' 를 삭제할까요?\n(Bake 다시 실행해야 지도에 반영됨)", "삭제", "취소")) return;
        string path = AssetDatabase.GetAssetPath(d);
        if (_editing == d) { _editing = null; _pts.Clear(); }
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        RefreshExisting(); Repaint();
        Debug.Log($"[RiverPainter] 삭제 {path}");
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
        Debug.Log($"[RiverPainter] 저장 {path} — kind={_kind}, width={_widthKm}km, 점 {_pts.Count}개");
    }
}
