using System;
using System.Collections.Generic;

namespace Checklist.Models
{
    public class ViewModel
    {
        public ViewModel()
        {
            ViewList = new List<ViewInfo>();
        }

        public List<ViewInfo> ViewList { get; set; }
    }

    public class ViewInfo
    {
        public ws_locationView Location { get; set; }
        public DateTime LastVisit { get; set; }
    }
}
