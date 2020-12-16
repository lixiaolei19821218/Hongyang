using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hongyang.Model
{
    /// <summary>
    /// 保存NC程序的名称，坐标系和角度
    /// </summary>
    class NCOutput
    {
        public int Angle { get; set; }

        public string Workplane => "U" + Angle;

        public string NC => "NC" + Angle;       
    }
}
