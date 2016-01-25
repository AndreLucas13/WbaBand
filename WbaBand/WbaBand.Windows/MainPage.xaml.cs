using Microsoft.Band;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Sensors;
using WbaBand.Global;
using WbaBand.Wrapper;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WbaBand.MicrosoftBand;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WbaBand
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IBandClient _bandClient;
        private IBandInfo _bandInfo;
        private BandController c;
        private double med;
        private double heartbeatmed=0;
        private int interv =0;
        private bool firstTime = true;
        private long totalCal;
        private BandReativeExtensionsWrapper bandHelper;
        private string separator = " , ";
        private bool btrange = false;

        public MainPage()
        {
            this.InitializeComponent();
            localFolderBox.Text = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            c = new BandController();
            //c.waitForLogin();
            Loaded += OnLoaded;
            
        }

        private async void OnLoaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ComboBoxItem typeItem = (ComboBoxItem)ComboBox1.SelectedItem;
            string valueCombo = typeItem.Content.ToString();
            interv = Convert.ToInt32(valueCombo);

            StartLogs();

            BandManager.GetInstance();
            BandManager.GetInstance().Initialize();
            BandManager.Done += (source, args) =>
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
               {
                   switch (args.EventInfo.channelName)
                   {
                       case "Accelerometer":
                           AcceDisplay.Text = "Accelerometer: " + args.EventInfo.sensorMessage;                          
                           break;

                       case "HeartRate":
                           HeartRateDisplay.Text = "HeartRate: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandHRLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " HeartRate: " + args.EventInfo.sensorMessage);
                           break;

                       case "SkinTemperature":
                           SkinTemperatureDisplay.Text = "Skin Temperature: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandSkinTempLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " Skin Temperature: " + args.EventInfo.sensorMessage);
                           break;

                       case "Contact":
                           ContactBandDisplay.Text = args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandContactLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " " + args.EventInfo.sensorMessage);
                           break;

                       case "Pedometer":
                           PedometerDisplay.Text = "Pedometer: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandPedometerLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " Pedometer: " + args.EventInfo.sensorMessage);
                           break;

                       case "UVIndex":
                           UVDisplay.Text = "UV: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandUVLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " UV: " + args.EventInfo.sensorMessage);
                           break;

                       case "Calories":
                           CaloriesDisplay.Text = "Calories: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandCaloriesLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " Calories: " + args.EventInfo.sensorMessage);
                           break;

                       case "Gsr":
                           GSRDisplay.Text = "Gsr: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandGsrLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " Gsr: " + args.EventInfo.sensorMessage);
                           break;

                       case "AmbientLight":
                           AmbientLightDisplay.Text = "AmbientLight: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandAmbientLightLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " AmbientLight: " + args.EventInfo.sensorMessage);
                           break;

                       case "Altimeter":
                           AltimeterDisplay.Text = "Altimeter: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandAltimeterLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " Altimeter: " + args.EventInfo.sensorMessage);
                           break;

                       case "Barometer":
                           BarometerDisplay.Text = args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandBarometerLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " " + args.EventInfo.sensorMessage);
                           break;

                       case "RRInterval":
                           RrIntervalDisplay.Text = "RRInterval: " + args.EventInfo.sensorMessage;
                           await c.saveStringToLocalFile("BandRRIntervalLogs.txt", "TimeStamp: " + args.EventInfo.timeStamp + " RRInterval: " + args.EventInfo.sensorMessage);
                           break;

                       default:
                           Debug.WriteLine("Default case - Error on reading sensors info");
                           break;

                   }
               }).AsTask();              
            };

        

            
            

            //bandHelper = new BandReativeExtensionsWrapper();

            //var bands = await BandClientManager.Instance.GetBandsAsync();
            //if (bands.Length < 1)
            //{
            //    var t = new MessageDialog("Can't find the Band", "Failed to Connect").ShowAsync();
            //    return;
            //}

            //await bandHelper.Connect();
            //ManageBand(bandHelper);

        }

        private async void StartLogs()
        {           

            await c.saveStringToLocalFile("BandHRLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandSkinTempLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandContactLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandPedometerLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandUVLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandCaloriesLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandGsrLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandAmbientLightLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandAltimeterLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandBarometerLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

            await c.saveStringToLocalFile("BandRRIntervalLogs.txt", "----- Application started at " + BandController.CurrentDate() + " ---");

        }

        public async void StartListening()
        {
            bandHelper = new BandReativeExtensionsWrapper();
        }      

        private async void ManageBand(BandReativeExtensionsWrapper bandHelper)
        {
            bandHelper.SendMessage("Testing Messages");

            var streamHR = await bandHelper.GetHeartRateStream();
            var streamCal = await bandHelper.GetCaloriesStream();
            var streamSkin = await bandHelper.GetSkinTemperatureStream();
            var streamUV = await bandHelper.GetUltravioletStream();
            var streamPed = await bandHelper.GetPedometerStream();
            var streamAcce = await bandHelper.GetAccelerometerStream();

            var currCalstream = await bandHelper.GetTotalCal();

            totalCal = await currCalstream.FirstAsync();

            await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandCalLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + "Ciclo de trabalho iniciado. Calorias desde o ultimo factory reset: " + totalCal);



            var hrSubscription = streamHR
                           .Where(x=>x.SensorReading.Quality==Microsoft.Band.Sensors.HeartRateQuality.Locked)                                                     
                           .Buffer(new TimeSpan(0, 0, interv))
                           .Select(async x =>
                           {
                               var avg = x.Average(y => y.SensorReading.HeartRate);
                               var min = x.Min(y => y.SensorReading.HeartRate);
                               var max = x.Max(y => y.SensorReading.HeartRate);
                               await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                               {
                                   HeartRateDisplay.Text = " HeartRate Med: " + Math.Round(avg, 2) + " Min: " + min + " Max: " + max;

                                   await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandHRLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + HeartRateDisplay.Text);
                               }).AsTask();
                               return avg;
                           })
                           .Subscribe(x =>
                           {
                               Debug.WriteLine(x);
                               if (x.IsFaulted)
                               {
                                   btrange = true;
                               }
                           });

            


            //var calSubscription = streamCal
            //                .Where(x => x.SensorReading.Calories != 0)
            //               .Buffer(new TimeSpan(0, 0, interv))
            //               .Select(async x =>
            //               {
            //                   var value = x.Select(y => y.SensorReading.Calories);
            //                   long cal = value.Last() - totalCal;

            //                   await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            //                   {
            //                       CaloriesDisplay.Text = "Calories: " + cal;

            //                       await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandCalLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + CaloriesDisplay.Text);
            //                   }).AsTask();
            //                   return cal;
            //               })
            //               .Subscribe(x =>
            //               {

            //                   Debug.WriteLine(x);

            //               });

            var skinSubscription = streamSkin
                           .Buffer(new TimeSpan(0, 0, interv))
                           .Select(async x =>
                           {
                               var avg = x.Average(y => y.SensorReading.Temperature);
                               var min = x.Min(y => y.SensorReading.Temperature);
                               var max = x.Max(y => y.SensorReading.Temperature);
                               await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                               {
                                   SkinTemperatureDisplay.Text = " Temperature Med: " + Math.Round(avg, 2) + " Min: " + min + " Max: " + max;

                                   await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandTemperatureLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + SkinTemperatureDisplay.Text);
                               }).AsTask();
                               return avg;
                           })
                           .Subscribe(x =>
                           {
                               Debug.WriteLine(x);

                           });

            //var pedSubscription = streamPed
            //               .Buffer(new TimeSpan(0, 0, interv))
            //               .Select(async x =>
            //               {
            //                   var total = x.Select(y => y.SensorReading.TotalSteps);
            //                   await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            //                   {
            //                       PedometerDisplay.Text = "Steps: " + total;

            //                       await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandStepsLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + PedometerDisplay.Text);
            //                   }).AsTask();
            //                   return total;
            //               })
            //               .Subscribe(x =>
            //               {
            //                   Debug.WriteLine(x);

            //               });

            //var uvSubscription = streamUV
            //               .Buffer(new TimeSpan(0, 0, interv))
            //               .Select(async x =>
            //               {
            //                   var index = x.Select(y => y.SensorReading.IndexLevel);
            //                   await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            //                   {
            //                       UVDisplay.Text = " UV: " + index.Last().ToString();

            //                       await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandUVLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + UVDisplay.Text);
            //                   }).AsTask();
            //                   return index;
            //               })
            //               .Subscribe(x =>
            //               {
            //                   Debug.WriteLine(x);

            //               });

            //var accSubscription = streamAcce
            //              .Buffer(new TimeSpan(0, 0, interv))
            //              .Select(async x =>
            //              {
            //                  var pointX = x.Select(y => y.SensorReading.AccelerationX);
            //                  var pointY = x.Select(y => y.SensorReading.AccelerationY);
            //                  var pointZ = x.Select(y => y.SensorReading.AccelerationZ);


            //                  await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            //                  {
            //                      AcceDisplay.Text = "Point X: " + pointX + "  Point Y: " + pointY + "  PointZ: " + pointZ;

            //                      await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandAccellLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + AcceDisplay.Text);
            //                  }).AsTask();
            //                  return pointX;
            //              })
            //              .Subscribe(x =>
            //              {
            //                  Debug.WriteLine(x);

            //              });
            //var streamAcc

            //if (_bandClient != null)
            //    return;

            //var bands = await BandClientManager.Instance.GetBandsAsync();
            //_bandInfo = bands.First();

            //_bandClient = await BandClientManager.Instance.ConnectAsync(_bandInfo);

            //var uc = _bandClient.SensorManager.HeartRate.GetCurrentUserConsent();
            //bool isConsented = false;
            //if (uc == UserConsent.NotSpecified)
            //{
            //    isConsented = await _bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
            //}

            //if (isConsented || uc == UserConsent.Granted)
            //{

            //    IEnumerable<TimeSpan> supportedHeartBeatReportingIntervals = _bandClient.SensorManager.HeartRate.SupportedReportingIntervals;
            //    foreach(var ri in supportedHeartBeatReportingIntervals)
            //    {

            //    }




            //    _bandClient.SensorManager.HeartRate.ReadingChanged += async (obj, ev) =>
            //    {
            //        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //                            {
            //                                HeartRateDisplay.Text = ev.SensorReading.HeartRate.ToString();
            //                                HeartRateState.Text = ev.SensorReading.Quality.ToString();
            //                                //await c.saveStringToLocalFile("BandHRLogs.txt", "HeartRate: " + ev.SensorReading.HeartRate);                            
            //                            }).AsTask();
            //    };
            //    await _bandClient.SensorManager.HeartRate.StartReadingsAsync();
            //}

        }

        private void alertHR(int hr)
        {
            if (hr > 85)
            {

            }


        }

        private void ComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (firstTime)
            {
                firstTime = false;
            }
            else {
                ComboBoxItem typeItem = (ComboBoxItem)ComboBox1.SelectedItem;
                string valueCombo = typeItem.Content.ToString();
                interv = Convert.ToInt32(valueCombo);
                BandManager.GetInstance().Interval = interv;
            }
        }

        private void checkCalories_Click(object sender, RoutedEventArgs e)
        {
            CheckCaloriesCycle();
        }

        
        private async void CheckCaloriesCycle()
        {
            var Calstream = await bandHelper.GetTotalCal();

            long currCal = await Calstream.FirstAsync();

            long diff = currCal - totalCal;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                               {
                                   CaloriesDisplay.Text = " Calories: " + diff;

                                   await c.saveStringToLocalFile(BandController.CurrentDateForFile() + "-BandCalLogs.txt", "TimeStamp: " + BandController.GetUnixTimeStamp() + CaloriesDisplay.Text);
                               }).AsTask();
           
        }

        private void RabbitPublisher()
        {
            var connectionFactory = new ConnectionFactory();
            connectionFactory.HostName = "localhost";
            connectionFactory.UserName = "guest";
            connectionFactory.Password = "guest";

            try
            {
                using (var connection = connectionFactory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare("WBA_HealthStatus_Trigger", "fanout");
                    String fullmsg = "Band," + BandController.GetUnixTimeStamp() + "," + heartbeatmed;
                    var body = Encoding.UTF8.GetBytes(fullmsg);
                    channel.BasicPublish("WBA_HealthStatus_Trigger_Response", "", null, body);
                    channel.Close();
                }
            }
            catch (Exception)
            {

                // do stuff
            }


        }
    }
    }

