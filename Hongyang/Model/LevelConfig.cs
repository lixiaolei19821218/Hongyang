using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hongyang.Model
{
    /// <summary>
    /// 预定义的颜色放入层，并定义测量方法
    /// </summary>
    class LevelConfig
    {
        public string Level { get; set; }
        public string Method { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
    }
}
