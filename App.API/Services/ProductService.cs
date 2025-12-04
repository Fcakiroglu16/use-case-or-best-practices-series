namespace App.API.Services
{
    public class ProductService
    {

        public List<string> GetProductNames(int count)
        {
            List<string> products = new List<string>();
            for (int i = 0; i < count; i++)
            {
                products.Add($"Product {i}");
            }
            return products.Where(p => p.Contains("Product")).Select(p => p.ToUpper()).ToList();
        }
    }
}
