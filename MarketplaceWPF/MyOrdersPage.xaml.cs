using System.Windows;
using System.Windows.Controls;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class MyOrdersPage : Page
    {
        public MyOrdersPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        private void LoadOrders()
        {
            OrdersList.ItemsSource = DatabaseHelper.GetOrdersByUser(Session.CurrentUser.Id);
        }

        private void BtnShowQr_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string code = btn.Tag.ToString();
                var win = new QrWindow(code);
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
        }
    }
}