using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hongyang.ViewModel
{
    class TreeItem
    {
        public string Icon { get; set; }
        public string Name { get; set; }

        public List<TreeItem> TreeItems { get; set; } = new List<TreeItem>();
    }
}
