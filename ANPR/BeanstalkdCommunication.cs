using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Turbocharged.Beanstalk;

namespace ANPR
{
    class BeanstalkdCommunication
    {
        /// <summary>
        /// Reads "anpr-in" tube
        /// </summary>
        /// <returns>Data from tube as object</returns>
        public static async Task<JObject> ReadTubeAsync()
        {
            // Consummer connection to local host
            IConsumer consumer = await BeanstalkConnection.ConnectConsumerAsync("localhost:11300");

            // Watch "anpr-in" tube
            await consumer.WatchAsync("anpr-in");

            // Get data from tube
            Job<JObject> job = await consumer.ReserveAsync<JObject>();

            // Delete data from tube
            await consumer.DeleteAsync(job.Id);

            // Dispose consumer
            consumer.Dispose();

            // Return data from tube as object
            return job.Object;
        }

        /// <summary>
        /// Write data in "anpr-out" tube
        /// </summary>
        /// <param name="data">Data to be written</param>
        public static async void WriteTubeAsync(JObject data)
        {
            // Producer connection to local host
            IProducer producer = await BeanstalkConnection.ConnectProducerAsync("localhost:11300");

            // Use "anpr-out" tube
            await producer.UseAsync("anpr-out");

            // Put data in tube
            await producer.PutAsync(data, 5, TimeSpan.Zero, TimeSpan.FromSeconds(0));

            // Dispose producer
            producer.Dispose();
        }
    }
}
