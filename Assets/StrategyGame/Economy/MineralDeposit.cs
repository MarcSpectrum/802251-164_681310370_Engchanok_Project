using UnityEngine;
namespace Engchanok.StrategyGame
{
    public sealed class MineralDeposit : MonoBehaviour
    {
        public MineralStock Stock { get; private set; }
        public void Initialize(int amount) { Stock = new MineralStock(amount); }
        public int Extract(int capacity)
        {
            int amount = Stock.Extract(capacity);
            if (Stock.Remaining == 0) foreach (var r in GetComponentsInChildren<Renderer>()) r.material.color = Color.gray;
            return amount;
        }
    }
}
