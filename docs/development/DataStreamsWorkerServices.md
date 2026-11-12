# Per-worker services for Data Streams Monitoring

A process that hosts several long-running Kafka workers can give each worker a stable service identity for Data Streams Monitoring. Begin the scope inside the worker's async method, before constructing its consumer and before consuming or producing messages:

```csharp
using Datadog.Trace;

async Task RunAsync(CancellationToken cancellationToken)
{
    using var workerService = DataStreams.BeginServiceScope("orders-worker");
    using var consumer = CreateConsumer();
    using var producer = CreateProducer();

    while (!cancellationToken.IsCancellationRequested)
    {
        var message = consumer.Consume(cancellationToken);
        using var processing = Tracer.Instance.StartActive("orders.process");
        processing.Span.ServiceName = "orders-worker";
        await ProcessAsync(message, producer, cancellationToken);
    }
}
```

The scope flows across `await` and is restored when disposed. Use a fixed, low-cardinality name for each logical worker, not a topic partition, message ID, tenant, or other unbounded value. The scope does not change the process's `DD_SERVICE`. Kafka produce and consume spans created inside it use the scoped service; other spans retain their normal service unless explicitly assigned, as in the processing span above. Kafka delivery reports capture the producing operation's service, and automatic commit callbacks use the service active when the consumer was constructed. Construct each consumer inside the intended worker scope and do not share it across differently named workers.

This feature requires both the fork's `Datadog.Trace` manual instrumentation package and a matching automatic tracer installation. The NuGet package alone does not install the automatic tracer. Enable Data Streams Monitoring and Kafka instrumentation in the automatic tracer as usual. Existing applications that do not create a worker service scope retain their configured service behavior.

The service scope applies to Kafka spans and Data Streams checkpoints and backlog. It does not automatically rename arbitrary application spans or establish per-worker service attribution for other integrations or business transaction tracking. Trace-to-profile correlation continues to use the process runtime identity and span context. Profiles are not guaranteed to appear as separate worker services; per-worker profile presentation in the Datadog UI has not been validated.
