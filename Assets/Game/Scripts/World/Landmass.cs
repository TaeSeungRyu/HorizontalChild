using Game.Data;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 씬에 spawn 된 대륙 한 덩어리. ShipController 의 충돌 검사가 본 컴포넌트를 인식.
    /// LandmassPlacer 가 자동 spawn — 사용자가 직접 부착할 일은 없음.
    /// </summary>
    public class Landmass : MonoBehaviour
    {
        public LandmassData Data { get; private set; }

        public void Bind(LandmassData data)
        {
            Data = data;
            if (data != null) name = $"Landmass_{data.landmassId}";
        }
    }
}
