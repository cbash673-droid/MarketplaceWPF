using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class SellerPage : Page
    {
        private string _selectedImageFileName = null;

        private static readonly string ImagesFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

        public SellerPage()
        {
            InitializeComponent();

            if (!Session.IsSeller)
            {
                MessageBox.Show("Доступ запрещен!");
                NavigationService.Navigate(new CatalogPage());
                return;
            }

            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);

            if (Session.CurrentShop == null)
                Session.CurrentShop = DatabaseHelper.GetShopByOwner(Session.CurrentUser.Id);

            LoadCategories();
            RefreshUI();
        }

        private void LoadCategories()
        {
            CmbCategory.ItemsSource = DatabaseHelper.GetCategories();
            if (CmbCategory.Items.Count > 0)
                CmbCategory.SelectedIndex = 0;
        }

        private void RefreshUI()
        {
            if (Session.CurrentShop == null)
            {
                CreateShopPanel.Visibility = Visibility.Visible;
                ShopManagePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                CreateShopPanel.Visibility = Visibility.Collapsed;
                ShopManagePanel.Visibility = Visibility.Visible;
                TxtShopTitle.Text = $"Магазин: {Session.CurrentShop.Name}";
                LoadProducts();
                LoadPromoCodes();
            }
        }

        private void LoadProducts()
        {
            SellerProductsList.ItemsSource = DatabaseHelper.GetProductsByShop(Session.CurrentShop.Id);
        }

        private void LoadPromoCodes()
        {
            PromoList.ItemsSource = DatabaseHelper.GetPromoCodesByOwner(Session.CurrentUser.Id);
        }

        // ---------- Создание магазина ----------

        private void BtnCreateShop_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtShopName.Text))
            {
                MessageBox.Show("Введите название магазина!");
                return;
            }
            var shop = DatabaseHelper.CreateShop(TxtShopName.Text, TxtShopDesc.Text, Session.CurrentUser.Id);
            Session.CurrentShop = shop;
            MessageBox.Show("Магазин создан!");
            RefreshUI();
        }

        // ---------- Выбор фото ----------

        private void BtnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите изображение товара",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                string ext = Path.GetExtension(dlg.FileName);
                string newName = Guid.NewGuid().ToString("N") + ext;
                string destPath = Path.Combine(ImagesFolder, newName);

                File.Copy(dlg.FileName, destPath, overwrite: true);
                _selectedImageFileName = newName;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(destPath, UriKind.Absolute);
                bmp.EndInit();
                ImgPreview.Source = bmp;
                TxtNoImage.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить фото: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Добавление товара ----------

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) ||
                !decimal.TryParse(TxtPrice.Text, out decimal price) ||
                !int.TryParse(TxtStock.Text, out int stock))
            {
                MessageBox.Show("Заполните все поля корректно!");
                return;
            }

            int discount = 0;
            if (!string.IsNullOrWhiteSpace(TxtDiscount.Text))
            {
                if (!int.TryParse(TxtDiscount.Text, out discount) || discount < 0 || discount > 90)
                {
                    MessageBox.Show("Скидка должна быть числом от 0 до 90.");
                    return;
                }
            }

            int? categoryId = null;
            if (CmbCategory.SelectedItem is Category cat)
                categoryId = cat.Id;

            DatabaseHelper.AddProduct(
                TxtName.Text, TxtDesc.Text, price, stock,
                Session.CurrentShop.Id, _selectedImageFileName, categoryId, discount);

            MessageBox.Show("Товар добавлен!");

            TxtName.Text = TxtDesc.Text = TxtPrice.Text = TxtStock.Text = TxtDiscount.Text = "";
            _selectedImageFileName = null;
            ImgPreview.Source = null;
            TxtNoImage.Visibility = Visibility.Visible;

            LoadProducts();
        }

        // ---------- Редактирование товара ----------

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;

            int productId = int.Parse(btn.Tag.ToString());

            var products = (SellerProductsList.ItemsSource as IEnumerable<Product>)?.ToList()
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

        // ---------- Удаление товара ----------

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;

            int id = int.Parse(btn.Tag.ToString());
            if (MessageBox.Show("Удалить товар?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            DatabaseHelper.DeleteProduct(id);
            LoadProducts();
        }

        // ---------- Промокоды ----------

        private void BtnCreatePromo_Click(object sender, RoutedEventArgs e)
        {
            string code = TxtPromoCode.Text?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Введите код промокода!");
                return;
            }

            if (!int.TryParse(TxtPromoDiscount.Text, out int discount) || discount < 1 || discount > 90)
            {
                MessageBox.Show("Скидка должна быть числом от 1 до 90.");
                return;
            }

            bool ok = DatabaseHelper.CreatePromoCode(
                code, discount, Session.CurrentShop.Id, Session.CurrentUser.Id);

            if (ok)
            {
                MessageBox.Show("Промокод создан!");
                TxtPromoCode.Text = "";
                TxtPromoDiscount.Text = "";
                LoadPromoCodes();
            }
            else
            {
                MessageBox.Show("Такой код уже существует!");
            }
        }

        private void BtnDeletePromo_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn) || btn.Tag == null) return;

            int id = int.Parse(btn.Tag.ToString());
            if (MessageBox.Show("Удалить промокод?", "Подтверждение",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            DatabaseHelper.DeletePromoCode(id);
            LoadPromoCodes();
        }
    }
}