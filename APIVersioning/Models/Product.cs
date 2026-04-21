namespace APIVersioning.Models
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

        public string CategoryId { get; set; }
        public string StockLevel { get; set; }
        public string Slug { get; set; }
        public string Rating { get; set; }
        public string ReviewCount { get; set; }

    }
}
