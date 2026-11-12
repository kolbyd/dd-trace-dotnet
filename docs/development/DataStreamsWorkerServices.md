# Service scopes for Data Streams Monitoring

A process hosting multiple workers can assign a stable Data Streams Monitoring (DSM) service name to each worker's execution. Set the scope around the work, regardless of the transport it uses:

```csharp
using Datadog.Trace;

async Task ExecuteWorkerAsync(IWorker worker, CancellationToken cancellationToken)
{
    using var service = DataStreams.BeginServiceScope(worker.ServiceName);
    await worker.RunAsync(cancellationToken);
}
```

The scope flows across `await` and restores the previous name when disposed. Dispose nested scopes in reverse order in the same logical execution context. Use a fixed, low-cardinality name for each logical worker, not a message ID, tenant, or other unbounded value. DSM uses the current scope when recording checkpoints and backlog; without a scope it uses the configured service. Background callbacks or work that suppresses execution context may not inherit the worker scope.

The scope affects DSM service attribution. It does not change `DD_SERVICE`, APM span or Activity service names, or runtime and profile identity. Configure APM service naming separately if needed.

This API requires the `Datadog.Trace` manual instrumentation package and a matching automatic tracer installation. The package alone does not install the automatic tracer. Enable Data Streams Monitoring and the relevant transport instrumentation in the automatic tracer.
