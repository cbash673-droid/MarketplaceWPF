using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class AdminPage : Page
    {
        private Order _foundOrder;

        public AdminPage()
        {
            InitializeComponent();
            if (!Session.IsAdmin)
            {
                MessageBox.Show("Доступ запрещен!");
                NavigationService.Navigate(new CatalogPage());
                return;
            }
            LoadCategories();
            LoadProducts();
        }

        private void LoadCategories()
        {
            CmbCategory.ItemsSource = DatabaseHelper.GetCategories();
            if (CmbCategory.Items.Count > 0)
                CmbCategory.SelectedIndex = 0;
        }

        private void LoadProducts() => AdminProductsList.ItemsSource = DatabaseHelper.GetProducts();

        // ============ Выдача заказов ============

        private void BtnFindOrder_Click(object sender, RoutedEventArgs e)
        {
            string code = TxtOrderCode.Text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Введите код заказа!");
                return;
            }

            _foundOrder = DatabaseHelper.GetOrderByCode(code);

            if (_foundOrder == null)
            {
                OrderInfoPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show("Заказ с таким кодом не найден.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Код: {_foundOrder.OrderCode}");
            sb.AppendLine($"Дата: {_foundOrder.CreatedAtText}");
            sb.AppendLine($"Статус: {_foundOrder.StatusText}");
            sb.AppendLine($"Оплата: {_foundOrder.PaymentMethodText}");
            sb.AppendLine($"Адрес доставки: {(_foundOrder.DeliveryAddress ?? "—")}");
            sb.AppendLine($"Сумма: {_foundOrder.TotalText}");
            sb.AppendLine();
            sb.AppendLine("Состав:");
            foreach (var item in _foundOrder.Items)
                sb.AppendLine($"  • {item.ProductName} × {item.Quantity} = {item.PriceText}");

            TxtOrderInfo.Text = sb.ToString();
            OrderInfoPanel.Visibility = Visibility.Visible;

            BtnIssue.Visibility = _foundOrder.Status == "Pending"
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnIssue_Click(object sender, RoutedEventArgs e)
        {
            if (_foundOrder == null) return;

            if (MessageBox.Show($"Выдать заказ {_foundOrder.OrderCode}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            bool ok = DatabaseHelper.MarkOrderAsIssued(_foundOrder.OrderCode);

            if (ok)
            {
                MessageBox.Show("Заказ успешно выдан!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _foundOrder = DatabaseHelper.GetOrderByCode(_foundOrder.OrderCode);
                if (_foundOrder != null)
                {
                    TxtOrderInfo.Text = $"Код: {_foundOrder.OrderCode}\n" +
                                        $"Статус: {_foundOrder.StatusText}";
                    BtnIssue.Visibility = Visibility.Collapsed;
                }
                TxtOrderCode.Text = "";
            }
            else
            {
                MessageBox.Show("Не удалось выдать заказ. Возможно, он уже выдан.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ============ Товары ============

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) ||
                !decimal.TryParse(TxtPrice.Text, out decimal price) ||
                !int.TryParse(TxtStock.Text, out int stock))
            {
                MessageBox.Show("Заполните все поля корректно!");
                return;
            }

            int? categoryId = null;
            if (CmbCategory.SelectedItem is Category cat)
                categoryId = cat.Id;

            DatabaseHelper.AddProduct(TxtName.Text, TxtDesc.Text, price, stock,
                null, null, categoryId, 0);

            MessageBox.Show("Товар добавлен!");
            TxtName.Text = TxtDesc.Text = TxtPrice.Text = TxtStock.Text = "";
            LoadProducts();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;

            int productId = int.Parse(btn.Tag.ToString());

            var products = (AdminProductsList.ItemsSource as IEnumerable<Product>)?.ToList()
                           ?? new List<Product>();
            var product = products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return;

            var win = new EditProductWindow(product);
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
            {
                LoadProducts();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;

            int id = int.Parse(btn.Tag.ToString());
            if (MessageBox.Show("Удалить товар?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                DatabaseHelper.DeleteProduct(id);
                LoadProducts();
            }
        }
    }
}