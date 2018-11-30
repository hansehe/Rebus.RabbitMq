﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
#pragma warning disable 1998

namespace Rebus.RabbitMq.Tests
{
    [TestFixture]
    public class RabbitMqPublisherConfirmsPublishPerformanceTest : FixtureBase
    {
        const string ConnectionString = RabbitMqTransportFactory.ConnectionString;

        /// <summary>
        /// Without confirms: 15508.5 msg/s
        ///
        /// With confirms
        ///     - initial:                      645 msg/s
        ///     - move call to ConfirmSelect:   815.0 msg/s
        /// 
        /// </summary>
        [TestCase(true, 10000)]
        [TestCase(false, 10000)]
        public async Task PublishBunchOfMessages(bool enablePublisherConfirms, int count)
        {
            var queueName = TestConfig.GetName("pub-conf");

            Using(new QueueDeleter(queueName));

            var activator = new BuiltinHandlerActivator();

            Using(activator);

            activator.Handle<string>(async str => { });

            Configure.With(activator)
                .Logging(l => l.Console(LogLevel.Info))
                .Transport(t => t.UseRabbitMq(ConnectionString, queueName)
                    .EnablePublisherConfirms(value: enablePublisherConfirms))
                .Start();

            var stopwatch = Stopwatch.StartNew();

            await Task.WhenAll(Enumerable.Range(0, count)
                .Select(n => $"THIS IS MESSAGE NUMBER {n} OUT OF {count}")
                .Select(str => activator.Bus.SendLocal(str)));

            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine($@"Publishing 

    {count} 

messages with PUBLISHER CONFIRMS = {enablePublisherConfirms} took 

    {elapsedSeconds:0.0} s

- that's {count/elapsedSeconds:0.0} msg/s");
        }
    }
}