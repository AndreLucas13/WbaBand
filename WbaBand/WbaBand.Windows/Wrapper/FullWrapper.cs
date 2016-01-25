using Microsoft.Band;
using Microsoft.Band.Notifications;
using Microsoft.Band.Sensors;
using Microsoft.Band.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace WbaBand.Wrapper
{
    class BandReativeExtensionsWrapper
    {
        public IBandClient Client;
        
        public int RemainingSpace;

        public async Task Connect()
        {
            
                try
                {
                // This method will throw an exception upon failure for a veriety of reasons,
                // such as Band out of range or turned off.
                //await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                //{
                    var bands = await BandClientManager.Instance.GetBandsAsync();
                    Client = await BandClientManager.Instance.ConnectAsync(bands[0]);
                //});
                }
                catch (Exception ex)
                {
                    var t = new MessageDialog(ex.Message, "Failed to Connect").ShowAsync();
                }
        }

        public async Task<IObservable<BandSensorReadingEventArgs<IBandHeartRateReading>>> GetHeartRateStream()
        {
            return await GetSensorStream<IBandHeartRateReading>(Client.SensorManager.HeartRate);
        }

        public async Task<IObservable<BandSensorReadingEventArgs<IBandAccelerometerReading>>> GetAccelerometerStream()
        {
            return await GetSensorStream<IBandAccelerometerReading>(Client.SensorManager.Accelerometer);
        }

        public async Task<IObservable<BandSensorReadingEventArgs<IBandUVReading>>> GetUltravioletStream()
        {
            return await GetSensorStream<IBandUVReading>(Client.SensorManager.UV);
        }

        public async Task<IObservable<BandSensorReadingEventArgs<IBandSkinTemperatureReading>>> GetSkinTemperatureStream()
        {
            return await GetSensorStream<IBandSkinTemperatureReading>(Client.SensorManager.SkinTemperature);
        }

        public async Task<IObservable<BandSensorReadingEventArgs<IBandCaloriesReading>>> GetCaloriesStream()
        {
            return await GetSensorStream<IBandCaloriesReading>(Client.SensorManager.Calories);
        }

        public async Task<IObservable<BandSensorReadingEventArgs<IBandDistanceReading>>> GetDistanceStream()
        {
            return await GetSensorStream<IBandDistanceReading>(Client.SensorManager.Distance);
        }
        public async Task<IObservable<BandSensorReadingEventArgs<IBandPedometerReading>>> GetPedometerStream()
        {
            return await GetSensorStream<IBandPedometerReading>(Client.SensorManager.Pedometer);
        }

        public async Task<IObservable<BandSensorReadingEventArgs<IBandGsrReading>>> GetGsrStream()
        {
            return await GetSensorStream<IBandGsrReading>(Client.SensorManager.Gsr);
        }   

        public async Task<IObservable<long>> GetTotalCal()
        {
            var calStream = await GetCaloriesStream();

            return calStream.Select(x => x.SensorReading.Calories);
            
        }

        public async Task<IObservable<TapEvent>> GetShakeOrTapStream(double threshold = 4, int timeSampleMs = 300)
        {
            var accStream = await GetAccelerometerStream();
            return accStream
                //Filter out any empty readings, band seems to report every other reading as all 0's
                .Where(x => x.SensorReading.AccelerationX != 0)
                //Get our readings, don't need the event args
                .Select(x => x.SensorReading)
                //Scan over the readings creating an aggregate reading for motion on all axis. 
                //Output all axis and the aggregate motion 
                .Scan<IBandAccelerometerReading, ChangeInMotion>(null, (last, current) =>
                {
                    //If we're the first the change is 0
                    if (last == null)
                    {
                        return new ChangeInMotion(0, current);
                    }

                    //Get difference in motion on all axis vs last reading as positive # then sum to get aggregate change in motion.
                    var aggregateChangeInMotion = (last.Reading.AccelerationX - current.AccelerationX) * -1 + (last.Reading.AccelerationY - current.AccelerationY) * -1 + (last.Reading.AccelerationZ - current.AccelerationZ) * -1;

                    return new ChangeInMotion(aggregateChangeInMotion, current);
                })
                //Collect a set of results over a timespan, around 400ms worked for me to detect taps and shakes
                .Buffer(new TimeSpan(0, 0, 0, 0, timeSampleMs))
                //Caculate the variance of the aggregate motion reading
                .Select(x =>
                {
                    var listOfAggregateMotion = x.Select(y => y.ChangeVsLastReading);
                    return new TapEvent(Variance(listOfAggregateMotion), x.Select(y => y.Reading));
                })
                .Where(x => x.Variance > threshold);
        }
        private double Variance(IEnumerable<double> nums)
        {
            if (nums.Count() > 1)
            {
                // Get the average of the values
                double avg = nums.Average();

                // Now figure out how far each point is from the mean
                // So we subtract from the number the average
                // Then raise it to the power of 2
                double sumOfSquares = 0.0;

                foreach (int num in nums)
                {
                    sumOfSquares += Math.Pow((num - avg), 2.0);
                }

                // Finally divide it by n - 1 (for standard deviation variance)
                // Or use length without subtracting one ( for population standard deviation variance)
                return sumOfSquares / (double)(nums.Count() - 1);
            }
            else { return 0.0; }
        }



        public async Task<IObservable<BandSensorReadingEventArgs<T>>> GetSensorStream<T>(IBandSensor<T> manager) where T : IBandSensorReading
        {
            var consent = manager.GetCurrentUserConsent();
            if (consent != UserConsent.Granted)
            {
                await manager.RequestUserConsentAsync();
            }

            var supportedIntervals = manager.SupportedReportingIntervals;
            manager.ReportingInterval = supportedIntervals.First();

            var stream = CreateObservableFromSensorEvent<T>(manager);

            return stream;
        }

        private IObservable<BandSensorReadingEventArgs<T>> CreateObservableFromSensorEvent<T>(IBandSensor<T> manager) where T : IBandSensorReading
        {

            var obs = Observable.FromEvent<
                EventHandler<BandSensorReadingEventArgs<T>>,
                BandSensorReadingEventArgs<T>>
                (
                    handler =>
                    {
                        EventHandler<BandSensorReadingEventArgs<T>> kpeHandler = (sender, e) => handler(e);
                        return kpeHandler;
                    },
                    async x =>
                    {
                        try
                        {                     
                            manager.ReadingChanged += x;
                            await manager.StartReadingsAsync();
                        }
                        catch (Exception)
                        {

                        }
                    },
                    async x =>
                    {
                        try
                        {
                            manager.ReadingChanged -= x;
                            await manager.StopReadingsAsync();
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                );
            return obs;
        }

        private async Task<IEnumerable<BandTile>> GetTiles()
        {
            IEnumerable<BandTile> tiles = await Client.TileManager.GetTilesAsync();
            return tiles;
        }

        private async Task<bool> AddTile()
        {
            return false;
        }

        private async void GetTileCapacity()
        {
            this.RemainingSpace = await this.Client.TileManager.GetRemainingTileCapacityAsync();
        }

        public async void SendMessage(string message)
        {
            var notifictionManager = Client.NotificationManager;

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

        public class ChangeInMotion
        {
            public ChangeInMotion(double ChangeVsLastReading, IBandAccelerometerReading Reading)
            {
                this.ChangeVsLastReading = ChangeVsLastReading;
                this.Reading = Reading;
            }
            public double ChangeVsLastReading { get; set; }
            public IBandAccelerometerReading Reading { get; set; }
        }

        public class TapEvent
        {
            public TapEvent(double Variance, IEnumerable<IBandAccelerometerReading> Readings)
            {
                this.Variance = Variance;
                this.Readings = Readings;
            }
            public double Variance { get; set; }
            public IEnumerable<IBandAccelerometerReading> Readings { get; set; }
        }

        public class CaloriesEvent
        {
            public CaloriesEvent(int total, IEnumerable<IBandCaloriesReading> Readings)
            {
                this.total = total;
                this.Readings = Readings;
            }

            public int total
            {
                get; set;
            }

            public IEnumerable<IBandCaloriesReading> Readings { get; set; }
        }

        /// <summary>
        /// Creates an observable sequence that only receives Band sensor readings when the Band is worn by the user.
        /// </summary>
        /// <typeparam name="T">Type of the Band sensor readings exposed by the observable sequence.</typeparam>
        /// <param name="sensor">The Band sensor observable sequence to receive readings from when the Band is worn by the user.</param>
        /// <param name="contact">The observable sequence for the contact sensor of the Band.</param>
        /// <returns>Observable sequence that only receives Band sensor readings when the Band is worn by the user.</returns>
        //public static IObservable<T> OnlyWhenWorn<T>(this IObservable<T> sensor, IObservable<IBandContactReading> contact)
        //{
        //    if (sensor == null)
        //    {
        //        throw new ArgumentNullException("sensor");
        //    }

        //    if (contact == null)
        //    {
        //        throw new ArgumentNullException("contact");
        //    }

        //    //
        //    // Switch between the specified sensor and the never sequence based on changes to contact state.
        //    //
        //    return contact.Select(c => c.State == BandContactState.Worn ? sensor : Observable.Never<T>()).Switch();
        //}
    }
}
