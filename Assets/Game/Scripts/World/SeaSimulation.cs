using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 바다 시뮬레이션 전역 정지 토큰.
    ///
    /// 항구 진입 / 일시정지 메뉴 등 여러 source 가 동시에 pause 가능 — 참조 카운팅으로
    /// 한 source 가 release 해도 다른 source 가 pause 중이면 계속 멈춤.
    ///
    /// 효과: Time.timeScale = 0 으로 Time.time 자체가 멈춰서 모든 Update 의 시간 계산
    /// (`_patrolPhaseEndsAt = Time.time + N` 같은 타이머) 이 자동으로 정지.
    /// </summary>
    public static class SeaSimulation
    {
        private static readonly HashSet<object> _pausers = new();

        public static bool IsPaused => _pausers.Count > 0;

        public static void Pause(object source)
        {
            if (source == null) return;
            bool wasEmpty = _pausers.Count == 0;
            _pausers.Add(source);
            if (wasEmpty) Time.timeScale = 0f;
        }

        public static void Resume(object source)
        {
            if (source == null) return;
            if (_pausers.Remove(source) && _pausers.Count == 0)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>씬 재로드 / 새 게임 시 모든 pause source 정리. 정적 상태 누수 방지.</summary>
        public static void Reset()
        {
            _pausers.Clear();
            Time.timeScale = 1f;
        }
    }
}
