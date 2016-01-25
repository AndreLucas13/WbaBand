using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace WbaBand.Global
{
    class BandController
    {
        public BandController()
        {

        }

        public async Task saveStringToLocalFile(string filename, string content)
        {
            // saves the string 'content' to a file 'filename' in the app's local storage folder
            //byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(content.ToCharArray());

            // create a file with the given filename in the local folder or open if it exists
            StorageFile file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(filename,CreationCollisionOption.OpenIfExists);

            await Windows.Storage.FileIO.AppendTextAsync(file, content + "\r\n");

            // write the char array created from the content string into the file
            //using (var stream = await file.OpenStreamForWriteAsync())
            //{
            //    stream.Write(fileBytes, file.Properties., fileBytes.Length);
            //}
        }
        

        public static string CurrentDate()
        {
            string time;
            time = DateTime.Now.ToString("yyyy/MM/dd/HH:mm:ss");
            return time;
        }

        public static string CurrentDateForFile()
        {
            string time;
            time = DateTime.Now.ToString("yyyy-MM-dd");
            return time;
        }

        public static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        public static string GetUnixTimeStamp()
        {

            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string s = unixTimestamp.ToString();
            return s;
        }

        public void waitForLogin()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            try
            {
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {

                    //channel.ExchangeDeclare("exchange", ExchangeType.Fanout);
                    channel.QueueDeclare("logsBand", false, false, true, null);
                    channel.QueueBind("logsBand", "exchange", "UserLogin");

                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume("logsBand", true, consumer);

                    var eventArgs = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                    if (eventArgs.Body != null)
                    {
                        string message = Encoding.UTF8.GetString(eventArgs.Body, 0, 0);

                    }
                }
            }
            catch (Exception)
            {
                waitForLogin();
            }

        }
    }
}
