using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class EditProductWindow : Window
    {
        private readonly Product _product;
        private string _selectedImageFileName;

        private static readonly string ImagesFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

        public EditProductWindow(Product product)
        {
            InitializeComponent();
            _product = product;

            if (!Directory.Exists(ImagesFolder))
                Directory.CreateDirectory(ImagesFolder);

            LoadCategories();
            FillFields();
        }

        private void LoadCategories()
        {
            CmbCategory.ItemsSource = DatabaseHelper.GetCategories();
        }

        private void FillFields()
        {
            TxtName.Text = _product.Name;
            TxtDesc.Text = _product.Description;
            TxtPrice.Text = _product.Price.ToString("0.##");
            TxtStock.Text = _product.Stock.ToString();
            TxtDiscount.Text = _product.DiscountPercent.ToString();

            if (_product.CategoryId.HasValue)
            {
                foreach (var item in CmbCategory.Items)
                {
                    if (item is Category c && c.Id == _product.CategoryId.Value)
                    {
                        CmbCategory.SelectedItem = c;
                        break;
                    }
                }
            }
            if (CmbCategory.SelectedItem == null && CmbCategory.Items.Count > 0)
                CmbCategory.SelectedIndex = 0;

            _selectedImageFileName = _product.ImagePath;
            if (!string.IsNullOrWhiteSpace(_product.ImagePath))
            {
                string full = Path.Combine(ImagesFolder, _product.ImagePath);
                if (File.Exists(full))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(full, UriKind.Absolute);
                    bmp.EndInit();
                    ImgPreview.Source = bmp;
                    TxtNoImage.Visibility = Visibility.Collapsed;
                }
            }
        }

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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
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

            try
            {
                DatabaseHelper.UpdateProduct(
                    _product.Id,
                    TxtName.Text,
                    TxtDesc.Text,
                    price,
                    stock,
                    _selectedImageFileName,
                    categoryId,
                    discount);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}