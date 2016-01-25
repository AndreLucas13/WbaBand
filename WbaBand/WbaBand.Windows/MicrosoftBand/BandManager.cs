// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BandManager.cs" company="Microsoft">
//   MLDC 2016
// </copyright>
// <summary>
//   Defines the Microsoft Band Manager.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using WbaBand.Events;
using WbaBand.Global;
using WbaBand.Wrapper;

namespace WbaBand.MicrosoftBand
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Windows.UI.Xaml;

    using Microsoft.Band;
    using Microsoft.Band.Notifications;
    using Microsoft.Band.Sensors;
    using Microsoft.Band.Tiles;

    /// <summary>
    /// This manager is responsible for connecting to the Microsoft Band and starting the various sensor streams.
    /// </summary>
    public class BandManager : IDisposable
    {
        private IBandInfo selectedBand;

        private IBandClient bandClient;

        public bool IsConnected => this.bandClient != null;


        private readonly Dictionary<string, string> channels;

        private readonly List<IDisposable> disposableStreams;

        private ClientSensor sensorClient;

        private bool sentNewValue;

        private Task initializingTask;

        private static BandManager instance;

        private static DispatcherTimer connectionChecker;

        private static long firstStepsReading;

        private static bool isFirstStepsReading = true;

        private static long firstCaloriesReading;

        private static bool isFirstCaloriesReading = true;     

        private const int HighOutputInterval = 5;

        private const string Separator = " ";

        public int Interval { get; set; }

        public delegate void SensorEventHandler(object source, SensorEventArgs e);

        public static event SensorEventHandler Done;

        private BandManager()
        {

            this.channels = new Dictionary<string, string>();
            this.disposableStreams = new List<IDisposable>();
            this.sentNewValue = false;
            this.Interval = 15;
        }

        public static BandManager GetInstance()
        {
            if (instance == null)
            {
                instance = new BandManager();
                return instance;
            }

            instance.Dispose();
            isFirstCaloriesReading = true;
            isFirstStepsReading = true;
            instance = new BandManager();
            return instance;
        }

        public void Initialize()
        {
            if (!this.IsConnected && this.initializingTask == null)
            {
                this.initializingTask = this.InitAsync();
            }
        }

        private async Task InitAsync()
        {
            try
            {
                this.selectedBand = await this.FindDevicesAsync();
                if (this.selectedBand == null)
                {
                    return;
                }

                this.bandClient = await this.ConnectAsync();
                if (this.bandClient == null)
                {
                    return;
                }

                //await this.LoadBandRegistrySensor();
                //if (this.bandRegistrySensor == null)
                //{
                //    return;
                //}

                this.StartStreams();

                StartConnectionChecker();
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.Message);
            }
            finally
            {
                this.initializingTask = null;
            }
        }

        private static void StartConnectionChecker()
        {
            if (connectionChecker != null)
            {
                return;
            }

            connectionChecker = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 60)
            };
            connectionChecker.Tick += (sender, o) =>
            {
                if (!instance.sentNewValue)
                {
                    Debug.WriteLine(
                        "-------------------------------\nLOST BAND CONNECTION...RECONNECTING\n-------------------------------");
                    instance.Dispose();
                    instance = null;
                    GetInstance().Initialize();
                }
                else
                {
                    Debug.WriteLine(
                        $"-------------------------------\nCONNECTED TO {instance.selectedBand.Name}\n-------------------------------");
                    instance.sentNewValue = false;
                }
            };
            connectionChecker.Start();
        }

        private async Task<IBandInfo> FindDevicesAsync(string name = null)
        {
            var bands = await BandClientManager.Instance.GetBandsAsync();
            if (bands != null && bands.Length > 0)
            {
                return name == null ? bands[0] : bands.SingleOrDefault(b => b.Name.Equals(name));
            }

            return null;
        }

        private async Task<IBandClient> ConnectAsync()
        {
            var client = await BandClientManager.Instance.ConnectAsync(this.selectedBand);

            await client.NotificationManager.VibrateAsync(VibrationType.NotificationAlarm);

            return client;
        }

        //private async Task LoadBandRegistrySensor()
        //{
        //    var client = new ClientRegistryPeople();

        //    try
        //    {
        //        var personalSensors = await client.ListPersonalSensorsAsync(this.currentCredentials);

        //        this.bandRegistrySensor =
        //            personalSensors.FirstOrDefault(
        //                registrySensor => registrySensor.ExtraData["HardwareAddress"] == this.selectedBand.Name);

        //        if (this.bandRegistrySensor == null)
        //        {
        //            return;
        //        }

        //        foreach (string channelCode in this.bandRegistrySensor.ExtraData["DataChannels"])
        //        {
        //            string channelName;
        //            if (!FHIRDefinitions.Instance.FHIRToSensorTypes.TryGetValue(channelCode, out channelName))
        //            {
        //                continue;
        //            }

        //            this.channels.Add(channelName, channelCode);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex);
        //    }
        //    finally
        //    {
        //        client.Dispose();
        //    }
        //}

        private async void StartStreams()
        {
            this.sensorClient = new ClientSensor();

            //this.SendMessage("Teste WBA");

            var streamTasks = new List<Task>
            {
                this.StartAccelerometerStream(),
                this.StartHeartRateStream(),
                this.StartSkinTemperatureStream(),
                this.StartContactStream(),
                this.StartPedometerStream(),
                this.StartUVIndexStream(),
                this.StartCaloriesStream(),
                this.StartGSRStream(),
                this.StartAmbientLightStream(),
                this.StartAltimeterStream(),
                this.StartBarometerStream(),
                this.StartRRIntervalStream()
            };

            try
            {
                await Task.WhenAll(streamTasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task StartAccelerometerStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.Accelerometer);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.Accelerometer);
            ////var channelCodes = new List<string>
            ////{
            ////    this.channels[$"{channelName}X"],
            ////    this.channels[$"{channelName}Y"],
            ////    this.channels[$"{channelName}Z"]
            ////};
            //var sensorType = channelCodes[0].Split('-').FirstOrDefault();
            await stream.Buffer(new TimeSpan(0, 0, HighOutputInterval)).Select(
                async readings =>
                {
                    var accelerationX = string.Join(Separator,
                        readings.Select(
                            reading => reading.SensorReading.AccelerationX.ToString(CultureInfo.InvariantCulture)));
                    var accelerationY = string.Join(Separator,
                        readings.Select(
                            reading => reading.SensorReading.AccelerationY.ToString(CultureInfo.InvariantCulture)));
                    var accelerationZ = string.Join(Separator,
                        readings.Select(
                            reading => reading.SensorReading.AccelerationZ.ToString(CultureInfo.InvariantCulture)));

                    if (string.IsNullOrEmpty(accelerationX)
                        && string.IsNullOrEmpty(accelerationY)
                        && string.IsNullOrEmpty(accelerationZ))
                    {
                        return;
                    }

                    var message = accelerationX + " , " + accelerationY + " , " + accelerationZ;

                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), message);

                    //var sampleValues = new DataSampleDC
                    //{
                    //    SensorType = sensorType,
                    //    Values = new Dictionary<string, string>
                    //                              {
                    //                                  { channelCodes[0], accelerationX.ToString() },
                    //                                  { channelCodes[1], accelerationY.ToString() },
                    //                                  { channelCodes[2], accelerationZ.ToString() }
                    //                              }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task StartHeartRateStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.HeartRate);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.HeartRate);
            //var channelCode = this.channels[channelName];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    var lockedReadings =
                        readings.Where(reading => reading.SensorReading.Quality == HeartRateQuality.Locked);
                    if (!lockedReadings.Any())
                    {
                        return;
                    }

                    var avg = lockedReadings.Average(reading => reading.SensorReading.HeartRate);
                    var min = lockedReadings.Min(y => y.SensorReading.HeartRate);
                    var max = lockedReadings.Max(y => y.SensorReading.HeartRate);
                    var avgdouble = Math.Round(avg, 2);
                    var message = min.ToString() + " , " + avgdouble.ToString() + " , " + max.ToString();

                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), message);
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                      { { channelCode, avg.ToString(CultureInfo.InvariantCulture) } }
                    //};
                await this.SendNewSample(sample);
                });
        }

        private async Task StartSkinTemperatureStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.SkinTemperature);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.SkinTemperature);
            //var channelCode = this.channels[channelName];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var avg = readings.Average(reading => reading.SensorReading.Temperature);

                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), avg.ToString());
                    //var sampleValue = new DataSampleDC  
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                  { { channelCode, avg.ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task StartContactStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.Contact);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.Contact);
            //var channelCode = this.channels[channelName];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var worn = readings.All(reading => reading.SensorReading.State == BandContactState.Worn);
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), worn.ToString());
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                  { { channelCode, worn.ToString() } }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task StartPedometerStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.Pedometer);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.Pedometer);
            //var channelCode = this.channels[channelName];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var steps = readings.Max(reading => reading.SensorReading.TotalSteps);

                    if (isFirstStepsReading)
                    {
                        isFirstStepsReading = false;
                        firstStepsReading = steps;
                    }
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(),
                            (steps - firstStepsReading).ToString(CultureInfo.InvariantCulture));
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                  { { channelCode, (steps - firstStepsReading).ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await
                        this.SendNewSample(sample);
                });
        }

        private async Task StartUVIndexStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.UV);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = $"{nameof(this.bandClient.SensorManager.UV)}Index";
            //var channelCode = this.channels[channelName];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var indexLevel =
                        readings.GroupBy(reading => reading.SensorReading.IndexLevel)
                            .OrderByDescending(gr => gr.Count())
                            .Select(gr => gr.Key)
                            .First();
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), indexLevel.ToString());
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                    { { channelCode, indexLevel.ToString() } }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task StartCaloriesStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.Calories);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.Calories);
            //var channelCode = this.channels[channelName];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var calories = readings.Max(reading => reading.SensorReading.Calories);

                    if (isFirstCaloriesReading)
                    {
                        isFirstCaloriesReading = false;
                        firstCaloriesReading = calories;
                    }
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(),
                            (calories - firstCaloriesReading).ToString(CultureInfo.InvariantCulture));
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                    { { channelCode, (calories - firstCaloriesReading).ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await
                        this.SendNewSample(sample);
                });
        }

        private async Task StartGSRStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.Gsr);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.Gsr);
            //var channelCode = this.channels[channelName.ToUpper()];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var gsr = readings.Average(reading => reading.SensorReading.Resistance);
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), gsr.ToString(CultureInfo.InvariantCulture));
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                    { { channelCode, gsr.ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task StartAmbientLightStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.AmbientLight);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.AmbientLight);
            //var channelCode = this.channels[channelName];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var brightness = readings.Average(reading => reading.SensorReading.Brightness);
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(),
                            brightness.ToString(CultureInfo.InvariantCulture));
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                    { { channelCode, brightness.ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await
                        this.SendNewSample(sample);
                });
        }

        private async Task StartAltimeterStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.Altimeter);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.Altimeter);
            //var channelCode = this.channels[channelName.ToUpper()];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var rate = readings.Average(reading => reading.SensorReading.Rate);
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), rate.ToString(CultureInfo.InvariantCulture));
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                    { { channelCode, gsr.ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task StartBarometerStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.Barometer);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.Barometer);
            //var channelCode = this.channels[channelName.ToUpper()];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var airp = readings.Average(reading => reading.SensorReading.AirPressure);
                    var temp = readings.Average(reading => reading.SensorReading.Temperature);
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), "Air Pressure: " + airp.ToString(CultureInfo.InvariantCulture) + 
                        " Temperature: " + temp.ToString(CultureInfo.InvariantCulture));
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                    { { channelCode, gsr.ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task StartRRIntervalStream()
        {
            var stream = await this.GetSensorStream(this.bandClient.SensorManager.RRInterval);
            if (stream == null)
            {
                return;
            }

            this.disposableStreams.Add(stream.Subscribe());
            var channelName = nameof(this.bandClient.SensorManager.RRInterval);
            //var channelCode = this.channels[channelName.ToUpper()];
            await stream.Buffer(new TimeSpan(0, 0, Interval)).Select(
                async readings =>
                {
                    if (!readings.Any())
                    {
                        return;
                    }

                    var rr = readings.Average(reading => reading.SensorReading.Interval);                   
                    var sample = new SensorReadingDC(channelName, BandController.GetUnixTimeStamp(), rr.ToString(CultureInfo.InvariantCulture));
                    //var sampleValue = new DataSampleDC
                    //{
                    //    SensorType = channelCode,
                    //    Values = new Dictionary<string, string>
                    //                                    { { channelCode, gsr.ToString(CultureInfo.InvariantCulture) } }
                    //};
                    await this.SendNewSample(sample);
                });
        }

        private async Task<IObservable<BandSensorReadingEventArgs<T>>> GetSensorStream<T>(IBandSensor<T> bandSensor)
            where T : IBandSensorReading
        {
            if (!this.IsConnected || !bandSensor.IsSupported)
            {
                return null;
            }

            var consent = bandSensor.GetCurrentUserConsent();
            if (consent != UserConsent.Granted)
            {
                await bandSensor.RequestUserConsentAsync();
            }

            var supportedIntervals = bandSensor.SupportedReportingIntervals;
            bandSensor.ReportingInterval = supportedIntervals.First();

            var stream = CreateObservableFromSensorEvent(bandSensor);

            return stream;
        }

        private static IObservable<BandSensorReadingEventArgs<T>> CreateObservableFromSensorEvent<T>(
            IBandSensor<T> bandSensor) where T : IBandSensorReading
        {
            var obs =
                Observable.FromEvent<EventHandler<BandSensorReadingEventArgs<T>>, BandSensorReadingEventArgs<T>>(
                    handler =>
                    {
                        EventHandler<BandSensorReadingEventArgs<T>> kpeHandler = (sender, e) => handler(e);
                        return kpeHandler;
                    },
                    async x =>
                    {
                        try
                        {
                            bandSensor.ReadingChanged += x;
                            await bandSensor.StartReadingsAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    },
                    async x =>
                    {
                        try
                        {
                            bandSensor.ReadingChanged -= x;
                            await bandSensor.StopReadingsAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    });
            return obs;
        }

        private async Task<IEnumerable<BandTile>> GetTiles()
        {
            IEnumerable<BandTile> tiles = await bandClient.TileManager.GetTilesAsync();
            return tiles;
        }

        private async Task<bool> AddTile()
        {
            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                var pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    Debug.WriteLine(
                        "MainPage.xaml.cs | ButtonBase_OnClick | This sample app requires a Microsoft Band paired to your device.Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.");
                    return false;
                }

                // Connect to Microsoft Band.
                using (var bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    // Create a Tile with a TextButton on it.
                    var myTileId = new Guid("12408A60-13EB-46C2-9D24-F14BF6A033C6");
                    var myTile = new BandTile(myTileId)
                    {
                        Name = "My Tile",
                        TileIcon = await LoadIcon("ms-appx:///Assets/SampleTileIconLarge.png"),
                        SmallIcon = await LoadIcon("ms-appx:///Assets/SampleTileIconSmall.png")
                    };

                    // Remove the Tile from the Band, if present. An application won't need to do this everytime it runs. 
                    // But in case you modify this sample code and run it again, let's make sure to start fresh.
                    await bandClient.TileManager.RemoveTileAsync(myTileId);

                    // Create the Tile on the Band.
                    await bandClient.TileManager.AddTileAsync(myTile);

                    // Subscribe to Tile events.
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage.xaml.cs | ButtonBase_OnClick | " + ex);
            }
            return false;
        }

    private async Task<BandIcon> LoadIcon(string uri)
    {
        StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

        using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
        {
            WriteableBitmap bitmap = new WriteableBitmap(1, 1);
            await bitmap.SetSourceAsync(fileStream);
            return bitmap.ToBandIcon();
        }
    }   

        public async void SendMessage(string message)
        {
            var notifictionManager = bandClient.NotificationManager;

            IEnumerable<BandTile> tiles = await GetTiles();

            var tileId = tiles.GetEnumerator().Current.TileId;
            await notifictionManager.VibrateAsync(VibrationType.ThreeToneHigh);
            await notifictionManager.ShowDialogAsync(
                tileId,
                "Aviso WBA",
                message);

            // send a notification to the Band with a dialog as well as a page
            //await notifictionManager.SendMessageAsync(
            //    tileId,
            //    "Message Title",
            //    "This is the message body...",
            //    DateTime.Now,
            //    true);
        }


        private async Task SendNewSample(SensorReadingDC sample)
        {
            try
            {
                this.sentNewValue = true;
                
                if (Done != null)
                {
                    Done(this, new SensorEventArgs(true, sample));
                }

               bool x = await this.sensorClient.SendSampleAsync(sample.sensorMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void Dispose()
        {
            foreach (var disposableStream in this.disposableStreams)
            {
                disposableStream.Dispose();
            }

            this.selectedBand = null;
            this.bandClient?.Dispose();
            this.bandClient = null;
            //this.sensorClient?.Dispose();
            //this.sensorClient = null;
        }
    }
}
