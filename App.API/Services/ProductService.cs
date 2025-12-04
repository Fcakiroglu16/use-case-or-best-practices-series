namespace App.API.Services
{
    public class ProductService
    {

        public List<string> GetProductNames(int count)
        {
            List<string> products = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                products.Add($"PRODUCT {i}");
            }
            return products;
        }
    }
}
