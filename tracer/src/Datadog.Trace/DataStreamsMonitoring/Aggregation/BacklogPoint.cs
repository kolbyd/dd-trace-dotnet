// <copyright file="BacklogPoint.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

namespace Datadog.Trace.DataStreamsMonitoring.Aggregation;

internal readonly struct BacklogPoint
{
    public readonly string Tags;
    public readonly long Value;
    public readonly long TimestampNs;
    public readonly string? ServiceNameOverride;

    public BacklogPoint(string tags, long value, long timestampNs, string? serviceNameOverride = null)
    {
        Tags = tags;
        Value = value;
        TimestampNs = timestampNs;
        ServiceNameOverride = serviceNameOverride;
    }
}
