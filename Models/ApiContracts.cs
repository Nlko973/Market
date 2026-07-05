namespace Market.Models
{
    public class CaptchaResponse
    {
        public string captchaId { get; set; }
        public string question { get; set; }
        public int expiresInSeconds { get; set; }
    }

    public class ApiMessage
    {
        public string message { get; set; }
    }

    public class TokenResponse
    {
        public string accessToken { get; set; }
        public string tokenType { get; set; }
        public string role { get; set; }
        public string username { get; set; }
        public int userId { get; set; }
        public int storeId { get; set; }
    }

    public class ProductItem
    {
        public int id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string barcode { get; set; }
        public int quantity { get; set; }
        public decimal price { get; set; }
    }

    public class ProductListResponse
    {
        public ProductItem[] items { get; set; }
    }

    public class CategoryItem
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class CategoryListResponse
    {
        public CategoryItem[] items { get; set; }
    }

    public class ReportTotalsResponse
    {
        public int totalItems { get; set; }
        public decimal totalPrice { get; set; }
    }

    public class ReportPeriodItem
    {
        public string name { get; set; }
        public int quantitySold { get; set; }
        public decimal totalPrice { get; set; }
    }

    public class ReportPeriodResponse
    {
        public ReportPeriodItem[] items { get; set; }
        public int totalItems { get; set; }
        public decimal totalPrice { get; set; }
    }

    public class EmployeeItem
    {
        public string fullName { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string role { get; set; }
    }

    public class EmployeeListResponse
    {
        public EmployeeItem[] items { get; set; }
    }

    public class UserProfileResponse
    {
        public string fullName { get; set; }
        public string storeName { get; set; }
        public string role { get; set; }
    }

    public class SaleProductItem
    {
        public int productId { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }
        public int quantity { get; set; }
    }

    public class SaleProductListResponse
    {
        public SaleProductItem[] items { get; set; }
    }

    public class CheckoutResponse
    {
        public int receiptId { get; set; }
        public decimal totalPrice { get; set; }
    }
}
