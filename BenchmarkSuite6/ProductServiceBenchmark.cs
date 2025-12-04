using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace App.API.Benchmarks
{
    [MemoryDiagnoser]
    public class ProductServiceBenchmark
    {
        private App.API.Services.ProductService _productService;

        [GlobalSetup]
        public void Setup()
        {
            _productService = new App.API.Services.ProductService();
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public List<string> GetProductNames(int count)
        {
            return _productService.GetProductNames(count);
        }
    }
}