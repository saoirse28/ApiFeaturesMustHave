namespace Resilience.DTOs
{
    public class StockLevel
    {
        private string productId;
        private int quantity;
        private bool available;
        private bool isEstimated;

        public string ProductId => productId;
        public int Quantity => quantity;
        public bool IsEstimated => isEstimated; 
        public bool Available => available;
        public StockLevel(string productId, int quantity, bool available, bool isEstimated)
        {
            this.productId=productId;
            this.quantity=quantity;
            this.available=available;
            this.isEstimated=isEstimated;
        }
    }
}
