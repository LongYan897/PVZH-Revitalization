using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack
{
    public class CardPackEntry
    {
        public CardModel Card { get; set; } = new();
        public int Count { get; set; } = new();

        /// <summary>
        /// 获取卡片还有多少张能凑够数量
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public int GetRemainingCount(int count) => Count - count;
    }
}
