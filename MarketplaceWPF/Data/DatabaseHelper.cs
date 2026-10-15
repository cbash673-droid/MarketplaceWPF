using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace MarketplaceWPF.Data
{
    public static class DatabaseHelper
    {
        private static string connString = "Data Source=DANGEROUS\\SQLEXPRESS;Initial Catalog=MarketplaceDB;Integrated Security=True;Trust Server Certificate=True";

        private static SqlConnection GetConnection() => new SqlConnection(connString);

        // ================ ПОЛЬЗОВАТЕЛИ ================

        public static User Login(string username)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Username, Role, LastAddress FROM Users WHERE Username=@u", conn);
                cmd.Parameters.AddWithValue("@u", username);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return new User
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Role = reader.IsDBNull(2) ? "User" : reader.GetString(2),
                        LastAddress = reader.IsDBNull(3) ? null : reader.GetString(3)
                    };
            }
            return null;
        }

        public static bool Register(string username) => RegisterWithRole(username, "User");
        public static bool RegisterSeller(string username) => RegisterWithRole(username, "Seller");

        private static bool RegisterWithRole(string username, string role)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                try
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO Users (Username, Password, Role) VALUES (@u, NULL, @r); SELECT SCOPE_IDENTITY();", conn);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@r", role);
                    int userId = Convert.ToInt32(cmd.ExecuteScalar());

                    var cmdCart = new SqlCommand("INSERT INTO Carts (UserId) VALUES (@uid)", conn);
                    cmdCart.Parameters.AddWithValue("@uid", userId);
                    cmdCart.ExecuteNonQuery();
                    return true;
                }
                catch { return false; }
            }
        }

        // Обновить последний адрес пользователя
        public static void UpdateLastAddress(int userId, string address)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("UPDATE Users SET LastAddress = @a WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@a", (object)address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
        }

        // ================ КАТЕГОРИИ ================

        public static List<Category> GetCategories()
        {
            var list = new List<Category>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Name FROM Categories ORDER BY Name", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Category { Id = reader.GetInt32(0), Name = reader.GetString(1) });
                }
            }
            return list;
        }

        // ================ ТОВАРЫ ================

        public static List<Product> GetProducts(string search = null, int? categoryId = null)
        {
            var list = new List<Product>();
            using (var conn = GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT p.Id, p.Name, p.Description, p.Price, p.Stock, 
                           p.ShopId, s.Name, p.ImagePath, p.CategoryId, c.Name, p.DiscountPercent
                    FROM Products p
                    LEFT JOIN Shops s ON p.ShopId = s.Id
                    LEFT JOIN Categories c ON p.CategoryId = c.Id
                    WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(search))
                    sql += " AND (p.Name LIKE @q OR p.Description LIKE @q)";

                if (categoryId.HasValue)
                    sql += " AND p.CategoryId = @cid";

                sql += " ORDER BY p.Name";

                var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(search))
                    cmd.Parameters.AddWithValue("@q", "%" + search + "%");
                if (categoryId.HasValue)
                    cmd.Parameters.AddWithValue("@cid", categoryId.Value);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Product
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Stock = reader.GetInt32(4),
                        ShopId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        ShopName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        ImagePath = reader.IsDBNull(7) ? null : reader.GetString(7),
                        CategoryId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                        CategoryName = reader.IsDBNull(9) ? null : reader.GetString(9),
                        DiscountPercent = reader.IsDBNull(10) ? 0 : reader.GetInt32(10)
                    });
                }
            }
            return list;
        }

        public static List<Product> GetProductsByShop(int shopId)
        {
            var list = new List<Product>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT p.Id, p.Name, p.Description, p.Price, p.Stock, 
                           p.ShopId, s.Name, p.ImagePath, p.CategoryId, c.Name, p.DiscountPercent
                    FROM Products p
                    LEFT JOIN Shops s ON p.ShopId = s.Id
                    LEFT JOIN Categories c ON p.CategoryId = c.Id
                    WHERE p.ShopId = @sid", conn);
                cmd.Parameters.AddWithValue("@sid", shopId);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Product
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Stock = reader.GetInt32(4),
                        ShopId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        ShopName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        ImagePath = reader.IsDBNull(7) ? null : reader.GetString(7),
                        CategoryId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                        CategoryName = reader.IsDBNull(9) ? null : reader.GetString(9),
                        DiscountPercent = reader.IsDBNull(10) ? 0 : reader.GetInt32(10)
                    });
                }
            }
            return list;
        }

        public static void AddProduct(string name, string desc, decimal price, int stock,
                                      int? shopId = null, string imagePath = null,
                                      int? categoryId = null, int discountPercent = 0)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "INSERT INTO Products (Name, Description, Price, Stock, ShopId, ImagePath, CategoryId, DiscountPercent) " +
                    "VALUES (@n, @d, @p, @s, @sid, @img, @cid, @disc)", conn);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@d", desc);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@sid", (object)shopId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@img", (object)imagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cid", (object)categoryId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@disc", discountPercent);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateProduct(int id, string name, string desc, decimal price,
                                         int stock, string imagePath, int? categoryId, int discountPercent)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    UPDATE Products 
                    SET Name = @n, Description = @d, Price = @p, Stock = @s, 
                        ImagePath = @img, CategoryId = @cid, DiscountPercent = @disc
                    WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@d", desc);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@img", (object)imagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cid", (object)categoryId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@disc", discountPercent);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteProduct(int id)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmdCart = new SqlCommand("DELETE FROM CartItems WHERE ProductId=@id", conn);
                cmdCart.Parameters.AddWithValue("@id", id);
                cmdCart.ExecuteNonQuery();

                var cmd = new SqlCommand("DELETE FROM Products WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DecreaseStock(int productId, int quantity)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "UPDATE Products SET Stock = Stock - @q WHERE Id = @id AND Stock >= @q", conn);
                cmd.Parameters.AddWithValue("@q", quantity);
                cmd.Parameters.AddWithValue("@id", productId);
                int rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    throw new InvalidOperationException(
                        $"Недостаточно товара на складе для ProductId={productId}");
            }
        }

        // ================ МАГАЗИНЫ ================

        public static Shop GetShopByOwner(int ownerId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Name, Description, OwnerId FROM Shops WHERE OwnerId=@oid", conn);
                cmd.Parameters.AddWithValue("@oid", ownerId);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return new Shop
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        OwnerId = reader.GetInt32(3)
                    };
            }
            return null;
        }

        public static Shop CreateShop(string name, string desc, int ownerId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "INSERT INTO Shops (Name, Description, OwnerId) VALUES (@n, @d, @o); SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@d", desc);
                cmd.Parameters.AddWithValue("@o", ownerId);
                int id = Convert.ToInt32(cmd.ExecuteScalar());
                return new Shop { Id = id, Name = name, Description = desc, OwnerId = ownerId };
            }
        }

        // ================ ПРОМОКОДЫ ================

        public static List<PromoCode> GetPromoCodesByOwner(int ownerId)
        {
            var list = new List<PromoCode>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT pc.Id, pc.Code, pc.DiscountPercent, pc.ShopId, pc.OwnerId, pc.IsActive, s.Name
                    FROM PromoCodes pc
                    LEFT JOIN Shops s ON pc.ShopId = s.Id
                    WHERE pc.OwnerId = @oid
                    ORDER BY pc.Code", conn);
                cmd.Parameters.AddWithValue("@oid", ownerId);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PromoCode
                    {
                        Id = reader.GetInt32(0),
                        Code = reader.GetString(1),
                        DiscountPercent = reader.GetInt32(2),
                        ShopId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                        OwnerId = reader.GetInt32(4),
                        IsActive = reader.GetBoolean(5),
                        ShopName = reader.IsDBNull(6) ? "Все товары" : reader.GetString(6)
                    });
                }
            }
            return list;
        }

        public static bool CreatePromoCode(string code, int discountPercent, int? shopId, int ownerId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                try
                {
                    var cmd = new SqlCommand(@"
                        INSERT INTO PromoCodes (Code, DiscountPercent, ShopId, OwnerId, IsActive)
                        VALUES (@c, @d, @sid, @o, 1)", conn);
                    cmd.Parameters.AddWithValue("@c", code);
                    cmd.Parameters.AddWithValue("@d", discountPercent);
                    cmd.Parameters.AddWithValue("@sid", (object)shopId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@o", ownerId);
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch { return false; }
            }
        }

        public static void DeletePromoCode(int id)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("DELETE FROM PromoCodes WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static PromoCode ValidatePromoCode(string code)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT pc.Id, pc.Code, pc.DiscountPercent, pc.ShopId, pc.OwnerId, pc.IsActive, s.Name
                    FROM PromoCodes pc
                    LEFT JOIN Shops s ON pc.ShopId = s.Id
                    WHERE pc.Code = @c AND pc.IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@c", code);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return new PromoCode
                    {
                        Id = reader.GetInt32(0),
                        Code = reader.GetString(1),
                        DiscountPercent = reader.GetInt32(2),
                        ShopId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                        OwnerId = reader.GetInt32(4),
                        IsActive = reader.GetBoolean(5),
                        ShopName = reader.IsDBNull(6) ? "Все товары" : reader.GetString(6)
                    };
            }
            return null;
        }

        // ================ КОРЗИНА ================

        public static void AddToCart(int userId, int productId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id FROM Carts WHERE UserId=@uid", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                var cartObj = cmd.ExecuteScalar();
                if (cartObj == null) return;
                int cartId = (int)cartObj;

                var cmdCheck = new SqlCommand("SELECT Id, Quantity FROM CartItems WHERE CartId=@cid AND ProductId=@pid", conn);
                cmdCheck.Parameters.AddWithValue("@cid", cartId);
                cmdCheck.Parameters.AddWithValue("@pid", productId);
                var reader = cmdCheck.ExecuteReader();

                if (reader.Read())
                {
                    int itemId = reader.GetInt32(0);
                    int qty = reader.GetInt32(1);
                    reader.Close();

                    var cmdUpd = new SqlCommand(@"
                        UPDATE CartItems 
                        SET Quantity = @q 
                        WHERE Id=@id 
                          AND @q <= (SELECT Stock FROM Products WHERE Id = CartItems.ProductId)", conn);
                    cmdUpd.Parameters.AddWithValue("@q", qty + 1);
                    cmdUpd.Parameters.AddWithValue("@id", itemId);
                    cmdUpd.ExecuteNonQuery();
                }
                else
                {
                    reader.Close();
                    var cmdIns = new SqlCommand("INSERT INTO CartItems (CartId, ProductId, Quantity) VALUES (@cid, @pid, 1)", conn);
                    cmdIns.Parameters.AddWithValue("@cid", cartId);
                    cmdIns.Parameters.AddWithValue("@pid", productId);
                    cmdIns.ExecuteNonQuery();
                }
            }
        }

        public static List<CartItem> GetCartItems(int userId)
        {
            var list = new List<CartItem>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT ci.Id, ci.ProductId, p.Name, p.Price, ci.Quantity, p.Stock,
                           p.DiscountPercent, p.ShopId, s.Name
                    FROM CartItems ci
                    JOIN Products p ON ci.ProductId = p.Id
                    JOIN Carts c ON ci.CartId = c.Id
                    LEFT JOIN Shops s ON p.ShopId = s.Id
                    WHERE c.UserId = @uid", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CartItem
                    {
                        Id = reader.GetInt32(0),
                        ProductId = reader.GetInt32(1),
                        ProductName = reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Quantity = reader.GetInt32(4),
                        Stock = reader.GetInt32(5),
                        DiscountPercent = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                        ShopId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                        ShopName = reader.IsDBNull(8) ? null : reader.GetString(8)
                    });
                }
            }
            return list;
        }

        public static void IncreaseCartItem(int cartItemId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    UPDATE CartItems 
                    SET Quantity = Quantity + 1 
                    WHERE Id = @id 
                      AND Quantity < (SELECT Stock FROM Products WHERE Id = CartItems.ProductId)", conn);
                cmd.Parameters.AddWithValue("@id", cartItemId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DecreaseCartItem(int cartItemId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(
                    "UPDATE CartItems SET Quantity = Quantity - 1 WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", cartItemId);
                cmd.ExecuteNonQuery();

                var cmdDel = new SqlCommand("DELETE FROM CartItems WHERE Id = @id AND Quantity <= 0", conn);
                cmdDel.Parameters.AddWithValue("@id", cartItemId);
                cmdDel.ExecuteNonQuery();
            }
        }

        public static void SetCartItemQuantity(int cartItemId, int quantity)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                if (quantity <= 0)
                {
                    var cmdDel = new SqlCommand("DELETE FROM CartItems WHERE Id=@id", conn);
                    cmdDel.Parameters.AddWithValue("@id", cartItemId);
                    cmdDel.ExecuteNonQuery();
                    return;
                }

                var cmd = new SqlCommand(@"
                    UPDATE CartItems 
                    SET Quantity = @q 
                    WHERE Id = @id 
                      AND @q <= (SELECT Stock FROM Products WHERE Id = CartItems.ProductId)", conn);
                cmd.Parameters.AddWithValue("@q", quantity);
                cmd.Parameters.AddWithValue("@id", cartItemId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveCartItem(int cartItemId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("DELETE FROM CartItems WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@id", cartItemId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void ClearCart(int userId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("DELETE FROM CartItems WHERE CartId = (SELECT Id FROM Carts WHERE UserId=@uid)", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.ExecuteNonQuery();
            }
        }

        // ================ ЗАКАЗЫ ================

        public static Order CreateOrder(int userId, PromoCode promoCode = null,
                                        string paymentMethod = "Card", string deliveryAddress = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var items = new List<OrderItem>();
                var cmdItems = new SqlCommand(@"
                    SELECT ci.ProductId, p.Name, p.Price, ci.Quantity, p.DiscountPercent, p.ShopId
                    FROM CartItems ci
                    JOIN Products p ON ci.ProductId = p.Id
                    JOIN Carts c ON ci.CartId = c.Id
                    WHERE c.UserId = @uid", conn);
                cmdItems.Parameters.AddWithValue("@uid", userId);

                var reader = cmdItems.ExecuteReader();
                while (reader.Read())
                {
                    decimal basePrice = reader.GetDecimal(2);
                    int discount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                    decimal finalPrice = Math.Round(basePrice * (1 - discount / 100m), 2);

                    items.Add(new OrderItem
                    {
                        ProductId = reader.GetInt32(0),
                        ProductName = reader.GetString(1),
                        Price = finalPrice,
                        Quantity = reader.GetInt32(3)
                    });
                }
                reader.Close();

                if (items.Count == 0) return null;

                decimal subtotal = 0;
                foreach (var it in items) subtotal += it.Price * it.Quantity;

                decimal discountAmount = 0;
                if (promoCode != null)
                {
                    if (promoCode.ShopId.HasValue)
                    {
                        var itemShopIds = new Dictionary<int, int?>();
                        var cmdShops = new SqlCommand(@"
                            SELECT ci.ProductId, p.ShopId
                            FROM CartItems ci
                            JOIN Products p ON ci.ProductId = p.Id
                            JOIN Carts c ON ci.CartId = c.Id
                            WHERE c.UserId = @uid", conn);
                        cmdShops.Parameters.AddWithValue("@uid", userId);
                        var r2 = cmdShops.ExecuteReader();
                        while (r2.Read())
                            itemShopIds[r2.GetInt32(0)] = r2.IsDBNull(1) ? (int?)null : r2.GetInt32(1);
                        r2.Close();

                        decimal shopSubtotal = 0;
                        foreach (var it in items)
                        {
                            if (itemShopIds.TryGetValue(it.ProductId, out int? sid)
                                && sid == promoCode.ShopId)
                            {
                                shopSubtotal += it.Price * it.Quantity;
                            }
                        }

                        discountAmount = Math.Round(shopSubtotal * promoCode.DiscountPercent / 100m, 2);
                    }
                    else
                    {
                        discountAmount = Math.Round(subtotal * promoCode.DiscountPercent / 100m, 2);
                    }
                }

                decimal total = subtotal - discountAmount;
                if (total < 0) total = 0;

                string orderCode = GenerateOrderCode();

                var cmdOrder = new SqlCommand(@"
                    INSERT INTO Orders (UserId, OrderCode, Total, Status, PromoCode, DiscountAmount, PaymentMethod, DeliveryAddress) 
                    VALUES (@uid, @code, @total, 'Pending', @promo, @disc, @pay, @addr); 
                    SELECT SCOPE_IDENTITY();", conn);
                cmdOrder.Parameters.AddWithValue("@uid", userId);
                cmdOrder.Parameters.AddWithValue("@code", orderCode);
                cmdOrder.Parameters.AddWithValue("@total", total);
                cmdOrder.Parameters.AddWithValue("@promo", (object)promoCode?.Code ?? DBNull.Value);
                cmdOrder.Parameters.AddWithValue("@disc", discountAmount);
                cmdOrder.Parameters.AddWithValue("@pay", (object)paymentMethod ?? DBNull.Value);
                cmdOrder.Parameters.AddWithValue("@addr", (object)deliveryAddress ?? DBNull.Value);
                int orderId = Convert.ToInt32(cmdOrder.ExecuteScalar());

                foreach (var it in items)
                {
                    var cmdIns = new SqlCommand(@"
                        INSERT INTO OrderItems (OrderId, ProductId, ProductName, Price, Quantity)
                        VALUES (@oid, @pid, @pname, @price, @qty)", conn);
                    cmdIns.Parameters.AddWithValue("@oid", orderId);
                    cmdIns.Parameters.AddWithValue("@pid", it.ProductId);
                    cmdIns.Parameters.AddWithValue("@pname", it.ProductName);
                    cmdIns.Parameters.AddWithValue("@price", it.Price);
                    cmdIns.Parameters.AddWithValue("@qty", it.Quantity);
                    cmdIns.ExecuteNonQuery();
                }

                // Обновляем последний адрес пользователя
                if (!string.IsNullOrWhiteSpace(deliveryAddress))
                {
                    var cmdAddr = new SqlCommand("UPDATE Users SET LastAddress = @a WHERE Id = @id", conn);
                    cmdAddr.Parameters.AddWithValue("@a", deliveryAddress);
                    cmdAddr.Parameters.AddWithValue("@id", userId);
                    cmdAddr.ExecuteNonQuery();
                }

                return new Order
                {
                    Id = orderId,
                    UserId = userId,
                    OrderCode = orderCode,
                    CreatedAt = DateTime.Now,
                    Total = total,
                    Status = "Pending",
                    PromoCode = promoCode?.Code,
                    DiscountAmount = discountAmount,
                    PaymentMethod = paymentMethod,
                    DeliveryAddress = deliveryAddress,
                    Items = items
                };
            }
        }

        private static string GenerateOrderCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rnd = new Random();
            string part1 = new string(Enumerable.Repeat(chars, 4).Select(s => s[rnd.Next(s.Length)]).ToArray());
            string part2 = new string(Enumerable.Repeat(chars, 4).Select(s => s[rnd.Next(s.Length)]).ToArray());
            return $"TM-{part1}-{part2}";
        }

        public static List<Order> GetOrdersByUser(int userId)
        {
            var orders = new List<Order>();
            using (var conn = GetConnection())
            {
                conn.Open();

                var cmd = new SqlCommand(@"
                    SELECT Id, UserId, OrderCode, CreatedAt, Total, Status, 
                           PromoCode, DiscountAmount, PaymentMethod, DeliveryAddress
                    FROM Orders
                    WHERE UserId = @uid
                    ORDER BY CreatedAt DESC", conn);
                cmd.Parameters.AddWithValue("@uid", userId);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    orders.Add(new Order
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        OrderCode = reader.GetString(2),
                        CreatedAt = reader.GetDateTime(3),
                        Total = reader.GetDecimal(4),
                        Status = reader.GetString(5),
                        PromoCode = reader.IsDBNull(6) ? null : reader.GetString(6),
                        DiscountAmount = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                        PaymentMethod = reader.IsDBNull(8) ? null : reader.GetString(8),
                        DeliveryAddress = reader.IsDBNull(9) ? null : reader.GetString(9)
                    });
                }
                reader.Close();

                foreach (var o in orders)
                    o.Items = GetOrderItems(o.Id);
            }
            return orders;
        }

        public static List<OrderItem> GetOrderItems(int orderId)
        {
            var list = new List<OrderItem>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT Id, OrderId, ProductId, ProductName, Price, Quantity
                    FROM OrderItems WHERE OrderId = @oid", conn);
                cmd.Parameters.AddWithValue("@oid", orderId);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new OrderItem
                    {
                        Id = reader.GetInt32(0),
                        OrderId = reader.GetInt32(1),
                        ProductId = reader.GetInt32(2),
                        ProductName = reader.GetString(3),
                        Price = reader.GetDecimal(4),
                        Quantity = reader.GetInt32(5)
                    });
                }
            }
            return list;
        }

        public static Order GetOrderByCode(string code)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT Id, UserId, OrderCode, CreatedAt, Total, Status, 
                           PromoCode, DiscountAmount, PaymentMethod, DeliveryAddress
                    FROM Orders WHERE OrderCode = @code", conn);
                cmd.Parameters.AddWithValue("@code", code);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var o = new Order
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        OrderCode = reader.GetString(2),
                        CreatedAt = reader.GetDateTime(3),
                        Total = reader.GetDecimal(4),
                        Status = reader.GetString(5),
                        PromoCode = reader.IsDBNull(6) ? null : reader.GetString(6),
                        DiscountAmount = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7),
                        PaymentMethod = reader.IsDBNull(8) ? null : reader.GetString(8),
                        DeliveryAddress = reader.IsDBNull(9) ? null : reader.GetString(9)
                    };
                    reader.Close();
                    o.Items = GetOrderItems(o.Id);
                    return o;
                }
            }
            return null;
        }

        public static bool MarkOrderAsIssued(string code)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    UPDATE Orders SET Status = 'Issued' 
                    WHERE OrderCode = @code AND Status = 'Pending'", conn);
                cmd.Parameters.AddWithValue("@code", code);
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}