// <copyright file="DataStreamsBeginServiceScopeIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DataStreamsMonitoring;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.ManualInstrumentation.DataStreams;

/// <summary>Instruments the manual Data Streams service scope API.</summary>
[InstrumentMethod(
    AssemblyName = "Datadog.Trace.Manual",
    TypeName = "Datadog.Trace.DataStreams",
    MethodName = "BeginServiceScopeInternal",
    ReturnTypeName = "System.IDisposable",
    ParameterTypeNames = [ClrNames.String],
    MinimumVersion = "3.58.0",
    MaximumVersion = ManualInstrumentationConstants.MaxVersion,
    IntegrationName = ManualInstrumentationConstants.IntegrationName)]
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DataStreamsBeginServiceScopeIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(string serviceName) => new(scope: null, state: serviceName);

    internal static CallTargetReturn<IDisposable> OnMethodEnd<TTarget>(IDisposable returnValue, Exception? exception, in CallTargetState state)
        => new(exception is null && state.State is string serviceName ? DataStreamsServiceScope.Begin(serviceName) : returnValue);
}
