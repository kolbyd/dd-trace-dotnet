// <copyright file="DataStreamsCheckpointOptions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

namespace Datadog.Trace;

/// <summary>Options applied to a single manual Data Streams Monitoring checkpoint.</summary>
public sealed class DataStreamsCheckpointOptions
{
    /// <summary>Gets or sets the checkpoint service name. Null or empty uses the configured service.</summary>
    public string? ServiceNameOverride { get; set; }

    /// <summary>Gets or sets the message payload size in bytes. The default is zero.</summary>
    public long PayloadSizeBytes { get; set; }
}
