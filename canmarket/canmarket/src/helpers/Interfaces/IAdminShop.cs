namespace canmarket.src.helpers.Interfaces
{
    public interface IAdminShop
    {
        public bool IsAdminShop { get; set; }
        public bool MustStorePayment { get; set; }
        public bool ProvidesInfiniteStocks { get; set; }
    }
}
