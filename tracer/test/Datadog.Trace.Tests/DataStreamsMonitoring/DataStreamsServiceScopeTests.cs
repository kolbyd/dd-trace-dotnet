// <copyright file="DataStreamsServiceScopeTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.ManualInstrumentation.DataStreams;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DataStreamsMonitoring;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.DataStreamsMonitoring;

public class DataStreamsServiceScopeTests
{
    [Fact]
    public async Task NestedScopesFlowAcrossAwaitAndRestore()
    {
        DataStreamsServiceScope.CurrentServiceName.Should().BeNull();
        using (DataStreamsServiceScope.Begin("outer"))
        {
            await Task.Yield();
            DataStreamsServiceScope.CurrentServiceName.Should().Be("outer");
            using (DataStreamsServiceScope.Begin("inner"))
            {
                await Task.Yield();
                DataStreamsServiceScope.CurrentServiceName.Should().Be("inner");
            }

            DataStreamsServiceScope.CurrentServiceName.Should().Be("outer");
        }

        DataStreamsServiceScope.CurrentServiceName.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentWorkersAndDisposalAreIsolated()
    {
        using var parent = DataStreamsServiceScope.Begin("parent");
        Task[] tasks = [Worker("first"), Worker("second")];
        await Task.WhenAll(tasks);
        DataStreamsServiceScope.CurrentServiceName.Should().Be("parent");

        async Task Worker(string name)
        {
            using (DataStreamsServiceScope.Begin(name))
            {
                await Task.Yield();
                DataStreamsServiceScope.CurrentServiceName.Should().Be(name);
            }

            DataStreamsServiceScope.CurrentServiceName.Should().Be("parent");
        }
    }

    [Fact]
    public async Task DisposingInheritedScopeDoesNotDisposeItInParentContext()
    {
        using var scope = DataStreamsServiceScope.Begin("parent");
        await Task.Run(() =>
        {
            scope.Dispose();
            DataStreamsServiceScope.CurrentServiceName.Should().BeNull();
        });

        DataStreamsServiceScope.CurrentServiceName.Should().Be("parent");
        scope.Dispose();
        scope.Dispose();
        DataStreamsServiceScope.CurrentServiceName.Should().BeNull();
    }

    [Fact]
    public void BridgeReturnsScopeAndRestoresParent()
    {
        var state = CallTargetInvoker.BeginMethod<DataStreamsBeginServiceScopeIntegration, object, string>(null, "worker");
        var scope = CallTargetInvoker.EndMethod<DataStreamsBeginServiceScopeIntegration, object, IDisposable>(null, new Noop(), null, in state).GetReturnValue();
        DataStreamsServiceScope.CurrentServiceName.Should().Be("worker");
        scope.Dispose();
        DataStreamsServiceScope.CurrentServiceName.Should().BeNull();
    }

    private sealed class Noop : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
