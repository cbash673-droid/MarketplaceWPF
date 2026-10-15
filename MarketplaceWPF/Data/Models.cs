using System;
using System.Collections.Generic;

namespace MarketplaceWPF.Data
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string LastAddress { get; set; }
    }

    public class Shop
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int OwnerId { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int DiscountPercent { get; set; }
        public int? ShopId { get; set; }
        public int OwnerId { get; set; }
        public bool IsActive { get; set; }
        public string ShopName { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DiscountPercent { get; set; }
        public int Stock { get; set; }
        public int? ShopId { get; set; }
        public string ShopName { get; set; }
        public string ImagePath { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }

        public decimal FinalPrice => Math.Round(Price * (1 - DiscountPercent / 100m), 2);
        public bool HasDiscount => DiscountPercent > 0;

        public string PriceText => "$" + Price.ToString("N2");
        public string FinalPriceText => "$" + FinalPrice.ToString("N2");
        public string DiscountText => $"-{DiscountPercent}%";
        public string StockText => $"В наличии: {Stock} шт.";

        public string ImageFullPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImagePath)) return null;
                return System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Images", ImagePath);
            }
        }
    }

    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int DiscountPercent { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public int? ShopId { get; set; }
        public string ShopName { get; set; }

        public decimal FinalPrice => Math.Round(Price * (1 - DiscountPercent / 100m), 2);
        public bool HasDiscount => DiscountPercent > 0;
        public decimal Subtotal => Math.Round(FinalPrice * Quantity, 2);

        public string PriceText => "$" + Price.ToString("N2");
        public string FinalPriceText => "$" + FinalPrice.ToString("N2");
        public string SubtotalText => "$" + Subtotal.ToString("N2");
        public string DiscountText => $"-{DiscountPercent}%";
    }

    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string OrderCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public string PromoCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public string PaymentMethod { get; set; }   // "Card", "Cash", "Online"
        public string DeliveryAddress { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        public string StatusText => Status == "Issued" ? "Выдан" : "Ожидает выдачи";
        public string CreatedAtText => CreatedAt.ToString("dd.MM.yyyy HH:mm");
        public string TotalText => "$" + Total.ToString("N2");
        public string DiscountAmountText => "$" + DiscountAmount.ToString("N2");
        public bool HasPromo => !string.IsNullOrWhiteSpace(PromoCode) && DiscountAmount > 0;

        public string PaymentMethodText
        {
            get
            {
                switch (PaymentMethod)
                {
                    case "Card": return "💳 Банковская карта";
                    case "Cash": return "💵 Наличными при получении";
                    case "Online": return "📱 Онлайн-перевод (СБП)";
                    default: return "—";
                }
            }
        }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public string PriceText => "$" + Price.ToString("N2");
    }
}