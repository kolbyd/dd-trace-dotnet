# Manual Data Streams Monitoring checkpoints

A process hosting multiple workers can specify a stable service name for each manually recorded Data Streams Monitoring (DSM) checkpoint. The options apply only to the checkpoint created by that call. They do not change automatic Kafka checkpoints, backlog, APM span service names, or process-wide service and profiling identity. Use low-cardinality names such as a fixed worker name.

For a transport that is not automatically instrumented, propagate headers with the manual API. This example uses a dictionary as the carrier; use the transport's actual header collection and encoding in an application.

```csharp
using Datadog.Trace;
using System.Collections.Generic;

var options = new DataStreamsCheckpointOptions
{
    ServiceNameOverride = "orders-worker",
    PayloadSizeBytes = payload.Length,
};

// Send: create a span, then send the carrier alongside the payload.
using var sendScope = Tracer.Instance.StartActive("send");
var headers = new Dictionary<string, string>();
new SpanContextInjector().InjectIncludingDsm(
    headers, (carrier, key, value) => carrier[key] = value,
    sendScope.Span.Context, "custom", "orders", options);

// Receive: use the received headers before creating the consumer span.
var parent = new SpanContextExtractor().ExtractIncludingDsm(
    headers, (carrier, key) => carrier.TryGetValue(key, out var value)
        ? new[] { value } : System.Array.Empty<string>(),
    "custom", "orders", options);
using var receiveScope = Tracer.Instance.StartActive("receive", new SpanCreationSettings { Parent = parent });
```

Null options, or a null or empty service override, use the configured service. The default payload size is zero. These overloads adapt Go's per-checkpoint service and payload options to the existing .NET manual propagation API; they are not full Go API parity. Injection requires a usable span context, and extraction records a checkpoint only when a trace context is extracted. The existing Kafka temporary pathway recovery branch restores a pathway without creating another checkpoint, so options do not change that already-recorded checkpoint.

Call these methods once per manually instrumented message edge. If transport instrumentation already records that edge, adding a manual checkpoint can duplicate it; use the manual checkpoint for edges without an automatic checkpoint and account for the Kafka recovery behavior above. Automatic Kafka instrumentation continues to use the configured service.

Install the Datadog.Trace manual instrumentation package and a matching automatic tracer, and enable DSM. The manual package alone leaves these operations inert.
