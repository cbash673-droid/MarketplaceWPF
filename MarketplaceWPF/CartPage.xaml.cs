using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class CartPage : Page
    {
        private PromoCode _appliedPromo = null;

        public CartPage()
        {
            InitializeComponent();
            LoadCart();
            PrefillAddress();
        }

        private void PrefillAddress()
        {
            if (!string.IsNullOrWhiteSpace(Session.CurrentUser?.LastAddress))
            {
                TxtDeliveryAddress.Text = Session.CurrentUser.LastAddress;
            }
        }

        private void LoadCart()
        {
            var items = DatabaseHelper.GetCartItems(Session.CurrentUser.Id);
            CartList.ItemsSource = items;

            decimal subtotal = items.Sum(i => i.Subtotal);
            decimal discountAmount = 0;

            if (_appliedPromo != null)
                discountAmount = CalculatePromoDiscount(items, _appliedPromo);

            decimal total = subtotal - discountAmount;
            if (total < 0) total = 0;

            TxtSubtotal.Text = "$" + subtotal.ToString("N2");
            TxtTotal.Text = "$" + total.ToString("N2");

            // Обновлённый блок
            if (discountAmount > 0)
            {
                PromoDiscountRow.Visibility = Visibility.Visible;
                TxtPromoDiscountAmount.Text = "-$" + discountAmount.ToString("N2");
            }
            else
            {
                PromoDiscountRow.Visibility = Visibility.Collapsed;
            }
        }

        private decimal CalculatePromoDiscount(System.Collections.Generic.List<CartItem> items, PromoCode promo)
        {
            if (promo == null) return 0;

            if (promo.ShopId.HasValue)
            {
                decimal sum = items
                    .Where(i => i.ShopId.HasValue && i.ShopId.Value == promo.ShopId.Value)
                    .Sum(i => i.Subtotal);
                return Math.Round(sum * promo.DiscountPercent / 100m, 2);
            }
            else
            {
                decimal sum = items.Sum(i => i.Subtotal);
                return Math.Round(sum * promo.DiscountPercent / 100m, 2);
            }
        }

        private void BtnApplyPromo_Click(object sender, RoutedEventArgs e)
        {
            string code = TxtPromoCode.Text?.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(code))
            {
                TxtPromoStatus.Text = "Введите код";
                TxtPromoStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            var promo = DatabaseHelper.ValidatePromoCode(code);
            if (promo == null)
            {
                TxtPromoStatus.Text = "Промокод не найден";
                TxtPromoStatus.Foreground = System.Windows.Media.Brushes.Red;
                _appliedPromo = null;
                LoadCart();
                return;
            }

            var items = DatabaseHelper.GetCartItems(Session.CurrentUser.Id);
            decimal d = CalculatePromoDiscount(items, promo);

            if (d <= 0)
            {
                TxtPromoStatus.Text = "Промокод не подходит ни к одному товару в корзине";
                TxtPromoStatus.Foreground = System.Windows.Media.Brushes.Red;
                _appliedPromo = null;
                LoadCart();
                return;
            }

            _appliedPromo = promo;
            TxtPromoStatus.Text = $"Применён: -{promo.DiscountPercent}%";
            TxtPromoStatus.Foreground = System.Windows.Media.Brushes.Green;
            LoadCart();
        }

        private void BtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            int cartItemId = int.Parse((sender as Button).Tag.ToString());
            DatabaseHelper.IncreaseCartItem(cartItemId);
            LoadCart();
        }

        private void BtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            int cartItemId = int.Parse((sender as Button).Tag.ToString());
            DatabaseHelper.DecreaseCartItem(cartItemId);
            LoadCart();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            int cartItemId = int.Parse((sender as Button).Tag.ToString());
            if (MessageBox.Show("Удалить товар из корзины?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            DatabaseHelper.RemoveCartItem(cartItemId);
            LoadCart();
        }

        private void TxtQuantity_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyQuantityFromTextBox(sender as TextBox);
        }

        private void TxtQuantity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyQuantityFromTextBox(sender as TextBox);
                e.Handled = true;
            }
        }

        private void ApplyQuantityFromTextBox(TextBox tb)
        {
            if (tb == null) return;
            if (!int.TryParse(tb.Text, out int qty)) qty = 1;
            if (qty < 0) qty = 0;

            int cartItemId = int.Parse(tb.Tag.ToString());
            DatabaseHelper.SetCartItemQuantity(cartItemId, qty);
            LoadCart();
        }

        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            var items = DatabaseHelper.GetCartItems(Session.CurrentUser.Id);

            if (items.Count == 0)
            {
                MessageBox.Show("Корзина пуста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string address = TxtDeliveryAddress.Text?.Trim();
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Укажите адрес доставки!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDeliveryAddress.Focus();
                return;
            }

            string paymentMethod = "Card";
            if (RbCash.IsChecked == true) paymentMethod = "Cash";
            else if (RbOnline.IsChecked == true) paymentMethod = "Online";

            try
            {
                foreach (var item in items)
                {
                    DatabaseHelper.DecreaseStock(item.ProductId, item.Quantity);
                }

                var order = DatabaseHelper.CreateOrder(
                    Session.CurrentUser.Id, _appliedPromo, paymentMethod, address);

                if (order == null)
                {
                    MessageBox.Show("Не удалось создать заказ.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                DatabaseHelper.ClearCart(Session.CurrentUser.Id);

                Session.CurrentUser.LastAddress = address;

                MessageBox.Show(
                    $"Заказ {order.OrderCode} успешно оформлен!\n\n" +
                    $"Способ оплаты: {order.PaymentMethodText}\n" +
                    $"Адрес доставки: {address}\n\n" +
                    $"QR-код для получения заказа можно посмотреть на странице «Мои заказы».",
                    "Заказ оформлен", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new MyOrdersPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оформлении заказа: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}