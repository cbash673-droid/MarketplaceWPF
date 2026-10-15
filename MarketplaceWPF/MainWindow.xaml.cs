using System.Windows;
using System.Windows.Navigation;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new LoginPage());
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (Session.CurrentUser != null)
            {
                TopBar.Visibility = Visibility.Visible;
                TxtTopUser.Text = $"Вы вошли как: {Session.CurrentUser.Username} ({GetRoleName(Session.CurrentUser.Role)})";
                BtnBack.IsEnabled = MainFrame.NavigationService?.CanGoBack ?? false;
            }
            else
            {
                TopBar.Visibility = Visibility.Collapsed;
            }
        }

        private string GetRoleName(string role)
        {
            switch (role)
            {
                case "Admin": return "Администратор";
                case "Seller": return "Продавец";
                default: return "Покупатель";
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.NavigationService?.CanGoBack == true)
                MainFrame.NavigationService.GoBack();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из аккаунта?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            Session.CurrentUser = null;
            Session.CurrentShop = null;

            while (MainFrame.NavigationService?.CanGoBack == true)
                MainFrame.NavigationService.RemoveBackEntry();

            MainFrame.Navigate(new LoginPage());
        }
    }
}