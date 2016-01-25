using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WbaBand.MicrosoftBand
{
    class ClientSensor
    {
        public ClientSensor()
        {
            
        }

        public async Task<bool> SendSampleAsync(string message)
        {
            try
            {
                await Task.Run(() => Debug.WriteLine("Done Done"));
                return true;
            }
            catch(Exception x)
            {
                Debug.WriteLine(x.Message);
                return false;
            }
            
            
        }
    }
}
