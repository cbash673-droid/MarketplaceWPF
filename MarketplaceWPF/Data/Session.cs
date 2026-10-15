namespace MarketplaceWPF.Data
{
    public static class Session
    {
        public static User CurrentUser { get; set; }
        public static bool IsAdmin => CurrentUser?.Role == "Admin";
        public static bool IsSeller => CurrentUser?.Role == "Seller";
        public static Shop CurrentShop { get; set; }
    }
}