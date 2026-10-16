using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LightroomSync
{
    internal class Config
    {
        public string LocalFolder { get; set; }
        public string NetworkFolder { get; set; }

        public bool AutoCheckForUpdates { get; set; }

        public int CheckIntervalSeconds { get; set; } = 5;
        public int BackupsToKeep { get; set; } = 3;
        public bool Notifications { get; set; } = true;

        public Config() { 
            this.LocalFolder = "C:\\Users\\" + System.Environment.UserName + "\\Pictures\\Lightroom";
            this.NetworkFolder = "P:\\Lightroom";
            this.AutoCheckForUpdates = true;
        }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        
    }
}
