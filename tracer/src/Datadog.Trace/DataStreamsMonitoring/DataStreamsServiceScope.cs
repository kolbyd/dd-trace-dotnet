// <copyright file="DataStreamsServiceScope.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Threading;

namespace Datadog.Trace.DataStreamsMonitoring;

internal sealed class DataStreamsServiceScope : IDisposable
{
    private static readonly AsyncLocal<DataStreamsServiceScope?> Current = new();
    private readonly DataStreamsServiceScope? _parent;
    private readonly string _serviceName;

    private DataStreamsServiceScope(string serviceName)
    {
        _parent = Current.Value;
        _serviceName = serviceName;
        Current.Value = this;
    }

    internal static string? CurrentServiceName => Current.Value?._serviceName;

    internal static IDisposable Begin(string serviceName) => new DataStreamsServiceScope(serviceName);

    public void Dispose()
    {
        // Only restore in the execution context where this scope is current. Do not
        // mutate the scope itself: execution contexts may share the same instance.
        if (ReferenceEquals(Current.Value, this))
        {
            Current.Value = _parent;
        }
    }
}
