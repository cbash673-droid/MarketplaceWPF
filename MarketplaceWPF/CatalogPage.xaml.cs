using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class CatalogPage : Page
    {
        private int? _selectedCategoryId = null;

        public CatalogPage()
        {
            InitializeComponent();
            if (Session.IsAdmin) BtnAdmin.Visibility = Visibility.Visible;
            if (Session.IsSeller) BtnSeller.Visibility = Visibility.Visible;

            LoadCategories();
            LoadProducts();
            UpdatePlaceholder();
        }

        private void LoadCategories()
        {
            var cats = DatabaseHelper.GetCategories();
            var all = new List<Category> { new Category { Id = 0, Name = "Все категории" } };
            all.AddRange(cats);
            CmbCategory.ItemsSource = all;
            CmbCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            string search = TxtSearch.Text?.Trim();
            if (string.IsNullOrWhiteSpace(search)) search = null;

            ProductsList.ItemsSource = DatabaseHelper.GetProducts(search, _selectedCategoryId);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholder();
            LoadProducts();
        }

        private void UpdatePlaceholder()
        {
            if (TxtSearchPlaceholder == null) return;
            TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCategory.SelectedItem is Category c)
            {
                _selectedCategoryId = c.Id == 0 ? (int?)null : c.Id;
                LoadProducts();
            }
        }

        // ---------- Клик по карточке — открывает детали ----------

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Border border) || !(border.DataContext is Product product))
                return;

            // Если клик пришёлся на кнопку — игнорируем (кнопка обрабатывает своё сама)
            var original = e.OriginalSource as DependencyObject;
            while (original != null && original != border)
            {
                if (original is Button) return;
                original = System.Windows.Media.VisualTreeHelper.GetParent(original);
            }

            var win = new ProductDetailsWindow(product, AddProductToCart);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        // ---------- Кнопка "В корзину" ----------

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;

            int productId = int.Parse(btn.Tag.ToString());
            var products = (ProductsList.ItemsSource as IEnumerable<Product>)?.ToList()
                           ?? new List<Product>();
            var product = products.FirstOrDefault(p => p.Id == productId);

            if (product != null)
                AddProductToCart(product);
        }

        // Общая логика добавления (используется и в карточке, и в окне деталей)
        private void AddProductToCart(Product product)
        {
            if (product.Stock <= 0) return;

            DatabaseHelper.AddToCart(Session.CurrentUser.Id, product.Id);
            MessageBox.Show("Товар добавлен в корзину!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ---------- Навигация ----------

        private void BtnCart_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new CartPage());
        private void BtnMyOrders_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new MyOrdersPage());
        private void BtnSeller_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new SellerPage());
        private void BtnAdmin_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new AdminPage());
    }
}