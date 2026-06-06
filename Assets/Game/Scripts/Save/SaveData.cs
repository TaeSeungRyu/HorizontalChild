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
    }

    [Serializable]
    public class CargoSlot
    {
        public string productId;
        public int quantity;
    }
}
