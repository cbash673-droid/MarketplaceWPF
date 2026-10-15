using System;
using System.Windows;
using System.Windows.Media.Imaging;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class ProductDetailsWindow : Window
    {
        private readonly Product _product;
        private readonly Action<Product> _onAddToCart;

        // product — товар, onAddToCart — что сделать, если пользователь нажмёт "В корзину"
        public ProductDetailsWindow(Product product, Action<Product> onAddToCart = null)
        {
            InitializeComponent();
            _product = product;
            _onAddToCart = onAddToCart;

            FillData();
        }

        private void FillData()
        {
            TxtName.Text = _product.Name;
            TxtCategory.Text = string.IsNullOrWhiteSpace(_product.CategoryName) ? "—" : _product.CategoryName;
            TxtShop.Text = string.IsNullOrWhiteSpace(_product.ShopName) ? "—" : _product.ShopName;
            TxtFinalPrice.Text = _product.FinalPriceText;
            TxtStock.Text = _product.StockText;
            TxtDescription.Text = string.IsNullOrWhiteSpace(_product.Description)
                ? "(описание отсутствует)"
                : _product.Description;

            if (_product.HasDiscount)
            {
                TxtOldPrice.Text = _product.PriceText;
                TxtOldPrice.Visibility = Visibility.Visible;

                BadgeDiscount.Visibility = Visibility.Visible;
                TxtBadgeDiscount.Text = _product.DiscountText;
            }

            // Фото
            if (!string.IsNullOrWhiteSpace(_product.ImageFullPath) &&
                System.IO.File.Exists(_product.ImageFullPath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(_product.ImageFullPath, UriKind.Absolute);
                bmp.EndInit();
                ImgProduct.Source = bmp;
            }

            // Если товара нет в наличии — отключаем кнопку "В корзину"
            if (_product.Stock <= 0)
            {
                BtnAddToCart.IsEnabled = false;
                BtnAddToCart.Content = "Нет в наличии";
            }
        }

        private void BtnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            _onAddToCart?.Invoke(_product);
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}