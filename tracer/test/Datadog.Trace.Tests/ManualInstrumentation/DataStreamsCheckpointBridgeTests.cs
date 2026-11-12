// <copyright file="DataStreamsCheckpointBridgeTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

extern alias DatadogTraceManual;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.ManualInstrumentation.Propagators;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.DataStreamsMonitoring;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.TestHelpers.TestTracer;
using Datadog.Trace.Tests.DataStreamsMonitoring;
using FluentAssertions;
using Xunit;
using ManualOptions = DatadogTraceManual::Datadog.Trace.DataStreamsCheckpointOptions;

namespace Datadog.Trace.Tests.ManualInstrumentation;

[Collection(nameof(TracerInstanceTestCollection))]
[TracerRestorer]
public class DataStreamsCheckpointBridgeTests
{
    [Fact]
    public async Task OptionsAreCapturedForEachManualCheckpoint()
    {
        var settings = TracerSettings.Create(new() { { ConfigurationKeys.DataStreamsMonitoring.Enabled, true } });
        await using var tracer = TracerHelper.Create(settings);
        Tracer.UnsafeSetTracerInstance(tracer);
        var manager = tracer.TracerManager.DataStreamsManager;
        var writer = new DataStreamsManagerTests.DataStreamsWriterMock();
        var originalWriter = manager.ExchangeWriterForTests(writer);
        try
        {
            var headers = new Dictionary<string, string>();
            Action<Dictionary<string, string>, string, string> setter = (h, k, v) => h[k] = v;
            Func<Dictionary<string, string>, string, IEnumerable<string>> getter = (h, k) => h.TryGetValue(k, out var value) ? new[] { value } : Array.Empty<string>();
            var options = new ManualOptions { ServiceNameOverride = "producer", PayloadSizeBytes = 42 };
            var context = new SpanContext(traceId: 123, spanId: 456);
            CallTargetInvoker.BeginMethod<SpanContextInjectorInjectIncludingDsmWithOptionsIntegration, object, Dictionary<string, string>, Action<Dictionary<string, string>, string, string>, SpanContext, string, string, ManualOptions>(null, headers, setter, context, "custom", "orders", options);
            options.ServiceNameOverride = "consumer";
            options.PayloadSizeBytes = 84;
            var state = CallTargetInvoker.BeginMethod<SpanContextExtractorExtractIncludingDsmWithOptionsIntegration, object, Dictionary<string, string>, Func<Dictionary<string, string>, string, IEnumerable<string>>, string, string, ManualOptions>(null, headers, getter, "custom", "orders", options);
            state.State.Should().BeOfType<SpanContext>();
            options.ServiceNameOverride = "changed";
            options.PayloadSizeBytes = 999;
            writer.Points.Should().HaveCount(2);
            writer.Points.TryDequeue(out var send).Should().BeTrue();
            writer.Points.TryDequeue(out var receive).Should().BeTrue();
            send.ServiceNameOverride.Should().Be("producer");
            send.PayloadSizeBytes.Should().Be(42);
            receive.ServiceNameOverride.Should().Be("consumer");
            receive.PayloadSizeBytes.Should().Be(84);

            var defaultHeaders = new Dictionary<string, string>();
            CallTargetInvoker.BeginMethod<SpanContextInjectorInjectIncludingDsmWithOptionsIntegration, object, Dictionary<string, string>, Action<Dictionary<string, string>, string, string>, SpanContext, string, string, ManualOptions>(null, defaultHeaders, setter, new SpanContext(123, 456), "custom", "orders", null);
            CallTargetInvoker.BeginMethod<SpanContextExtractorExtractIncludingDsmWithOptionsIntegration, object, Dictionary<string, string>, Func<Dictionary<string, string>, string, IEnumerable<string>>, string, string, ManualOptions>(null, defaultHeaders, getter, "custom", "orders", new ManualOptions { ServiceNameOverride = string.Empty });
            writer.Points.TryDequeue(out var defaultSend).Should().BeTrue();
            writer.Points.TryDequeue(out var defaultReceive).Should().BeTrue();
            defaultSend.ServiceNameOverride.Should().BeNull();
            defaultReceive.ServiceNameOverride.Should().BeNull();
            defaultSend.PayloadSizeBytes.Should().Be(0);
            defaultReceive.PayloadSizeBytes.Should().Be(0);

            var legacyHeaders = new Dictionary<string, string>();
            CallTargetInvoker.BeginMethod<SpanContextInjectorInjectIncludingDsmIntegration, object, Dictionary<string, string>, Action<Dictionary<string, string>, string, string>, SpanContext, string, string>(null, legacyHeaders, setter, new SpanContext(123, 456), "custom", "orders");
            CallTargetInvoker.BeginMethod<SpanContextExtractorExtractIncludingDsmIntegration, object, Dictionary<string, string>, Func<Dictionary<string, string>, string, IEnumerable<string>>, string, string>(null, legacyHeaders, getter, "custom", "orders");
            writer.Points.TryDequeue(out var legacySend).Should().BeTrue();
            writer.Points.TryDequeue(out var legacyReceive).Should().BeTrue();
            legacySend.ServiceNameOverride.Should().BeNull();
            legacyReceive.ServiceNameOverride.Should().BeNull();
            legacySend.PayloadSizeBytes.Should().Be(0);
            legacyReceive.PayloadSizeBytes.Should().Be(0);

            var dsmOnly = new Dictionary<string, string> { ["dd-pathway-ctx-base64"] = headers["dd-pathway-ctx-base64"] };
            var noTrace = CallTargetInvoker.BeginMethod<SpanContextExtractorExtractIncludingDsmWithOptionsIntegration, object, Dictionary<string, string>, Func<Dictionary<string, string>, string, IEnumerable<string>>, string, string, ManualOptions>(null, dsmOnly, getter, "custom", "orders", options);
            noTrace.State.Should().BeNull();
            writer.Points.Should().BeEmpty();

            defaultHeaders[DataStreamsPropagationHeaders.TemporaryBase64PathwayContext] = "VGhlIHF1aWMWIGJyb3duIGZveCBqdW1wcyBvdmVyIDEzIGxhenkgZG9ncy4=";
            var recovered = CallTargetInvoker.BeginMethod<SpanContextExtractorExtractIncludingDsmIntegration, object, Dictionary<string, string>, Func<Dictionary<string, string>, string, IEnumerable<string>>, string, string>(null, defaultHeaders, getter, "custom", "orders");
            recovered.State.Should().BeOfType<SpanContext>();
            ((SpanContext)recovered.State).PathwayContext.Should().NotBeNull();
            writer.Points.Should().BeEmpty();
        }
        finally
        {
            manager.ExchangeWriterForTests(originalWriter);
        }
    }
}
