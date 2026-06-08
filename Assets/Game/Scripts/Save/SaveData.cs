using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Save
{
    /// <summary>
    /// JSON 저장용 직렬화 데이터 POCO.
    /// JsonUtility 는 [Serializable] + 공개 필드만 직렬화.
    ///
    /// 버전 정책 (§11.4):
    ///   - version 필드로 마이그레이션 지원
    ///   - 새 필드 추가 시 version 증가 + 로더에서 분기
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAtUtc; // ISO 8601, 표시·디버그용

        // 국가 선택
        public string nationId;

        // 진행 상태
        public List<string> discoveredIds = new();
        public List<string> completedMissionIds = new();
        public List<string> unlockedRegionIds = new();
        public string currentMissionId;

        // 플레이어 스탯
        public int money;
        public int goodReputation;
        public int badReputation;

        // 화물
        public List<CargoSlot> cargo = new();

        // 마지막 위치 (월드 좌표 직접)
        public float shipX;
        public float shipZ;

        // NPC 배 상태 — 위치 + 무역 항로 인덱스 (M3.5 Phase 1)
        public List<NpcStateData> npcs = new();

        // 격침 후 재추첨 대기 큐 (GAME_MECHANICS §3.4 — 패배 NPC 는 랜덤 항구로 재배치)
        public List<NpcRespawnEntry> npcRespawnQueue = new();
    }

    [Serializable]
    public class CargoSlot
    {
        public string productId;
        public int quantity;
    }

    [Serializable]
    public class NpcStateData
    {
        public string npcId;
        public float x;
        public float z;
        public int routeIndex;
    }

    [Serializable]
    public class NpcRespawnEntry
    {
        public string npcId;
        public float secondsRemaining;
    }
}
