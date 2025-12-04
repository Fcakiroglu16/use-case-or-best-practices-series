using System.Threading.Tasks;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using App.API;
using Microsoft.VSDiagnostics;

namespace BenchmarkSuite6
{
    [CPUUsageDiagnoser]
    public class ProductServiceBenchmark
    {
        private ProductService _service;
        [GlobalSetup]
        public void Setup() => _service = new ProductService();
        [Benchmark]
        public List<int> GetContentLengths_Sync() => _service.GetContentLengths();
        [Benchmark]
        public async Task GetContentLengths_Async()
        {
            await foreach (var len in _service.GetContentLengthsAsync())
            {
            // consume
            }
        }
    }
}