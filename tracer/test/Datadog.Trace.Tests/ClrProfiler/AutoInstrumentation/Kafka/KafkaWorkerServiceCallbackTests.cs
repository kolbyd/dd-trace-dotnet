// <copyright file="KafkaWorkerServiceCallbackTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Kafka;
using Datadog.Trace.Configuration;
using Datadog.Trace.DataStreamsMonitoring;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.TestHelpers.TestTracer;
using Datadog.Trace.Tests.DataStreamsMonitoring;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.ClrProfiler.AutoInstrumentation.Kafka;

[Collection(nameof(TracerInstanceTestCollection))]
[TracerRestorer]
public class KafkaWorkerServiceCallbackTests
{
    [Theory]
    [InlineData("worker-a")]
    [InlineData(null)]
    public async Task DelayedCallbacksUseCapturedService(string capturedService)
    {
        var settings = TracerSettings.Create(new() { { ConfigurationKeys.DataStreamsMonitoring.Enabled, "true" } });
        await using var tracer = TracerHelper.Create(settings);
        TracerRestorerAttribute.SetTracer(tracer);
        var manager = tracer.TracerManager.DataStreamsManager;
        var writer = new DataStreamsManagerTests.DataStreamsWriterMock();
        var originalWriter = manager.ExchangeWriterForTests(writer);
        try
        {
            var report = new Mock<IDeliveryReport>();
            report.SetupGet(x => x.Topic).Returns("orders");
            report.SetupGet(x => x.Partition).Returns(new Partition { Value = 1 });
            report.SetupGet(x => x.Offset).Returns(new Offset { Value = 42 });
            var partition = Mock.Of<ITopicPartition>();
            var message = new Mock<IMessage>();
            message.SetupGet(x => x.Value).Returns("message");
            message.SetupGet(x => x.Instance).Returns(new object());

            Datadog.Trace.ClrProfiler.CallTarget.CallTargetState asyncState;
            Action<IDeliveryReport> syncCallback;
            OffsetsCommittedCallbacks commitCallback;
            using (capturedService is null ? null : DataStreamsServiceScope.Begin(capturedService))
            {
                asyncState = KafkaProduceAsyncIntegration.OnMethodBegin(new object(), partition, message.Object, CancellationToken.None);
                asyncState.Scope.Close();
                var syncScope = KafkaHelper.CreateProducerScope(tracer, new object(), partition, false, false);
                syncCallback = KafkaProduceSyncDeliveryHandlerIntegration.WrapAction<IDeliveryReport>(_ => { }, syncScope.Span, DataStreamsServiceScope.CurrentServiceName);
                syncScope.Close();
                commitCallback = new OffsetsCommittedCallbacks("group", DataStreamsServiceScope.CurrentServiceName);
            }

            var item = new Mock<ITopicPartitionOffset>();
            item.SetupGet(x => x.Topic).Returns("orders");
            item.SetupGet(x => x.Partition).Returns(new Partition { Value = 1 });
            item.SetupGet(x => x.Offset).Returns(new Offset { Value = 42 });
            var offsets = new Mock<ITopicPartitionOffsets>();
            offsets.SetupGet(x => x.Count).Returns(1);
            offsets.SetupGet(x => x[0]).Returns(item.Object);
            var result = new Mock<ICommittedOffsets>();
            result.SetupGet(x => x.Offsets).Returns(offsets.Object);
            var consumer = new object();
            ICommittedOffsets committed = result.Object;

            using (DataStreamsServiceScope.Begin("other-worker"))
            {
                KafkaProduceAsyncIntegration.OnAsyncMethodEnd<object, IDeliveryReport>(new object(), report.Object, null, asyncState);
                syncCallback(report.Object);
                commitCallback.OnDelegateBegin(null, ref consumer, ref committed);
            }

            writer.BacklogPoints.Should().HaveCount(3);
            writer.BacklogPoints.Should().OnlyContain(x => x.ServiceNameOverride == capturedService);
        }
        finally
        {
            manager.ExchangeWriterForTests(originalWriter);
        }
    }
}
