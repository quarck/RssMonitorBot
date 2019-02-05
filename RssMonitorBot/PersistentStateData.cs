using System;
using System.Collections.Generic;
using System.Text;

namespace RssMonitorBot
{
    [Serializable]
    public class PersistentStateData
    {
        public int LastOffset = 0;
    }
}
