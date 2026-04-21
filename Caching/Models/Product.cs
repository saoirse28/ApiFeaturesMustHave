namespace Caching.Models
{
    public class Product
    {
        public Product(CreateProductRequest createProduct) 
        { 
            Id = Guid.NewGuid().ToString();
            Name        = createProduct.Name;
            Description = createProduct.Description;
            Price       = createProduct.Price;
            Category    = createProduct.Category;
        }

        public void Apply(UpdateProductRequest updateProduct)
        {
            Id          = updateProduct.Id;
            Name        = updateProduct.Name;
            Description = updateProduct.Description;
            Price       = updateProduct.Price;
            Category    = updateProduct.Category;
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }

    }
}
