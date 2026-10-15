using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace MarketplaceWPF
{
    public partial class QrWindow : Window
    {
        public QrWindow(string orderCode)
        {
            InitializeComponent();
            TxtCode.Text = orderCode;
            QrImage.Source = GenerateQr(orderCode);
        }

        private BitmapImage GenerateQr(string text)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            {
                var pngQr = new PngByteQRCode(qrData);
                byte[] pngBytes = pngQr.GetGraphic(10);

                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = new MemoryStream(pngBytes);
                img.EndInit();
                img.Freeze();
                return img;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}