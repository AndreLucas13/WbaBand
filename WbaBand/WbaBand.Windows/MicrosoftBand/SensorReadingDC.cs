using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WbaBand.MicrosoftBand
{
    public class SensorReadingDC
    {
        public string channelName { get; set; }
        public string timeStamp { get; set; }        
        public string sensorMessage { get; set; }

        public SensorReadingDC(string cN, string tS, string sM)
        {
            this.channelName = cN;
            this.timeStamp = tS;
            this.sensorMessage = sM;
        }        
            
        
    }
}
