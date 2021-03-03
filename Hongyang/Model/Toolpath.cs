using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hongyang.Model
{
    /// <summary>
    /// 保存刀路名，检测点数，孔数。PI生成报告用
    /// </summary>
    class Toolpath
    {
        public string Name{ get;set;}
        public int Point { get; set; }
        public int Hole { get; set; }
        public double ZAngle { get; set; }
    }
}
