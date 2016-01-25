using Microsoft.Band;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WbaBandShared;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WbaBand.TesteBand
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private BandSensors _sensors;
        private IDisposable _simpleHeartRateSubscription;
        private IDisposable _heartRateStatsSubscription;
        private IDisposable _skinTemperatureSubscription;
        private IDisposable _stepGoalsSubscription;
        private IDisposable _averageSpeedSubscription;

        public MainPage()
        {
            this.InitializeComponent();

        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //
                // Try to get the Band.
                //
                var pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    
                    txtConnect.Text = "This sample app requires a Microsoft Band paired to your phone. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }

                var pairedBand = pairedBands[0];

                //
                // Connect to the Band and get the sensors.
                //
                var bandClient = await BandClientManager.Instance.ConnectAsync(pairedBand);
                _sensors = new BandSensors(bandClient.SensorManager);

                //
                // Tweak UI.
                //
                //btnConnect.IsEnabled = false;
                //btnSimpleHeart.IsEnabled = true;
                //btnHeartStats.IsEnabled = true;
                //btnSkinTemperature.IsEnabled = true;
                //btnStepGoals.IsEnabled = true;
                //btnAverageSpeed.IsEnabled = true;
            }
            catch (Exception ex)
            {
                txtConnect.Text = ex.ToString();
            }
        }
    }
}
