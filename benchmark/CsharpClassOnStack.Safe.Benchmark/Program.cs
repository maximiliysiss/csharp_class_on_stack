using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using CsharpClassOnStack.Safe.Benchmark;
using CsharpClassOnStack.Safe.Benchmark.Sandbox;

_ = BenchmarkRunner.Run<SafeTest>();

return;

namespace CsharpClassOnStack.Safe.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net90)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [SimpleJob(RuntimeMoniker.Net70)]
    public class SafeTest
    {
        private readonly int _n = 1000;

        [Benchmark]
        public void SingleAllocateHeap()
        {
            var obj = new ExampleClass();

            obj.X = 1;
            obj.Y = 2;

            GC.KeepAlive(obj);
        }

        [Benchmark]
        public void SingleAllocateStack()
        {
            var obj = Stack<ExampleClass>.Safe(stackalloc byte[Stack<ExampleClass>.Size]);

            obj.X = 1;
            obj.Y = 2;
        }

        [Benchmark]
        public void ManyAllocateHeap()
        {
            for (var i = 0; i < _n; i++)
            {
                var obj = new ExampleClass();

                obj.X = 1;
                obj.Y = 2;

                GC.KeepAlive(obj);
            }
        }

        [Benchmark]
        public void ManyAllocateStack()
        {
            for (var i = 0; i < _n; i++)
            {
                var obj = Stack<ExampleClass>.Safe(stackalloc byte[Stack<ExampleClass>.Size]);

                obj.X = 1;
                obj.Y = 2;
            }
        }
    }
}
