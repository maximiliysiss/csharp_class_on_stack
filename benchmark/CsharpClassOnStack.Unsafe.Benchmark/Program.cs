using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using CsharpClassOnStack.Unsafe.Benchmark;
using CsharpClassOnStack.Unsafe.Benchmark.Sandbox;

_ = BenchmarkRunner.Run<UnsafeTest>();

return;

namespace CsharpClassOnStack.Unsafe.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net90)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [SimpleJob(RuntimeMoniker.Net70)]
    public class UnsafeTest
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
        public unsafe void SingleAllocateStack()
        {
            byte* buffer = stackalloc byte[Stack<ExampleClass>.Size];
            var obj = Stack<ExampleClass>.Unsafe(buffer);

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
        public unsafe void ManyAllocateStack()
        {
            for (var i = 0; i < _n; i++)
            {
                byte* buffer = stackalloc byte[Stack<ExampleClass>.Size];
                var obj = Stack<ExampleClass>.Unsafe(buffer);

                obj.X = 1;
                obj.Y = 2;
            }
        }
    }
}
