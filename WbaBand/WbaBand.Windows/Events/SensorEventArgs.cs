using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WbaBand.MicrosoftBand;

namespace WbaBand.Events
{
    public class SensorEventArgs : EventArgs
    {

        public SensorEventArgs(bool option, SensorReadingDC info = null)
        {
            this.EventOption = option;
            this.EventInfo = info;

            
        }

        /// <summary>
        /// Gets a value indicating whether event option.
        /// </summary>
        public bool EventOption { get; private set; }

        /// <summary>
        /// Gets the event info.
        /// </summary>
        public SensorReadingDC EventInfo { get; private set; }
    }
}
