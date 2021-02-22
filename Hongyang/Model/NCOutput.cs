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

        public string NC => Workplane;

        public string Name { get; set; }//存NC程序名

        public double ZAngle { get; set; }//存PM中计算得出的NC程序ZAngle值

        public int Point { get; set; }//存PM中NC程序的刀具点数之和
    }
}
