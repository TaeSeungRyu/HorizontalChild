using System.Collections.Generic;
using Game.Data;
using Game.Ship;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Player
{
    /// <summary>
    /// 플레이어 화물 인벤토리.
    /// productId 별로 보유 수량을 관리. 용량은 현재 ShipData.cargoCapacity 참조.
    ///
    /// 싱글톤. GameManager 또는 PlayerShip 옆에 부착 권장.
    ///
    /// M3 단순화: 메모리 only. Save 시스템 (§11.4) 도입 시 직렬화 추가.
    /// </summary>
    public class PlayerCargo : MonoBehaviour
    {
        public static PlayerCargo Instance { get; private set; }

        [Header("Refs")]
        [Tooltip("플레이어 배. 비어 있으면 자동 검색. cargoCapacity 조회용.")]
        public ShipController playerShip;

        [Tooltip("배가 없을 때의 기본 화물 용량.")]
        public int defaultCapacity = 100;

        [Header("Events")]
        public UnityEvent onCargoChanged;

        // productId → (ProductData 참조 + 수량)
        private readonly Dictionary<string, CargoEntry> _items = new();

        public int TotalQuantity { get; private set; }

        public int Capacity =>
            (playerShip != null && playerShip.shipData != null)
                ? playerShip.shipData.cargoCapacity
                : defaultCapacity;

        public int RemainingCapacity => Mathf.Max(0, Capacity - TotalQuantity);

        public struct CargoEntry
        {
            public ProductData data;
            public int quantity;
        }

        public IReadOnlyDictionary<string, CargoEntry> Items => _items;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (playerShip == null) playerShip = FindAnyObjectByType<ShipController>(FindObjectsInactive.Include);
        }

        public int GetQuantity(ProductData product)
        {
            if (product == null) return 0;
            return _items.TryGetValue(product.productId, out var item) ? item.quantity : 0;
        }

        public bool TryAdd(ProductData product, int amount)
        {
            if (product == null || amount <= 0) return false;
            if (TotalQuantity + amount > Capacity) return false;

            if (_items.TryGetValue(product.productId, out var item))
            {
                _items[product.productId] = new CargoEntry { data = item.data, quantity = item.quantity + amount };
            }
            else
            {
                _items[product.productId] = new CargoEntry { data = product, quantity = amount };
            }
            TotalQuantity += amount;
            onCargoChanged?.Invoke();
            return true;
        }

        public bool TryRemove(ProductData product, int amount)
        {
            if (product == null || amount <= 0) return false;
            if (!_items.TryGetValue(product.productId, out var item)) return false;
            if (item.quantity < amount) return false;

            int newQ = item.quantity - amount;
            if (newQ == 0) _items.Remove(product.productId);
            else _items[product.productId] = new CargoEntry { data = item.data, quantity = newQ };

            TotalQuantity -= amount;
            onCargoChanged?.Invoke();
            return true;
        }
    }
}
