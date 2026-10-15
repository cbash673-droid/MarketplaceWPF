using System.Windows;
using System.Windows.Input;
using MarketplaceWPF.Data;

namespace MarketplaceWPF
{
    public partial class LoginPage : System.Windows.Controls.Page
    {
        private bool isLoginMode = true;

        public LoginPage() { InitializeComponent(); }

        private void TxtToggle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isLoginMode = !isLoginMode;
            TitleText.Text = isLoginMode ? "Вход в аккаунт" : "Регистрация";
            BtnSubmit.Content = isLoginMode ? "Войти" : "Зарегистрироваться";
            TxtToggle.Text = isLoginMode ? "Нет аккаунта? Зарегистрироваться" : "Уже есть аккаунт? Войти";
            RolePanel.Visibility = isLoginMode ? Visibility.Collapsed : Visibility.Visible;
            TxtError.Visibility = Visibility.Collapsed;
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string user = TxtUsername.Text?.Trim();

            if (string.IsNullOrWhiteSpace(user))
            {
                ShowError("Введите логин"); return;
            }

            if (isLoginMode)
            {
                var loggedUser = DatabaseHelper.Login(user);
                if (loggedUser != null)
                {
                    Session.CurrentUser = loggedUser;
                    // Если продавец — подгружаем его магазин
                    if (loggedUser.Role == "Seller")
                        Session.CurrentShop = DatabaseHelper.GetShopByOwner(loggedUser.Id);

                    NavigationService.Navigate(new CatalogPage());
                }
                else ShowError("Пользователь с таким логином не найден");
            }
            else
            {
                bool ok = RbSeller.IsChecked == true
                    ? DatabaseHelper.RegisterSeller(user)
                    : DatabaseHelper.Register(user);

                if (ok)
                {
                    var loggedUser = DatabaseHelper.Login(user);
                    Session.CurrentUser = loggedUser;
                    if (loggedUser.Role == "Seller")
                        Session.CurrentShop = DatabaseHelper.GetShopByOwner(loggedUser.Id);

                    NavigationService.Navigate(new CatalogPage());
                }
                else ShowError("Пользователь с таким логином уже существует");
            }
        }

        private void ShowError(string msg) { TxtError.Text = msg; TxtError.Visibility = Visibility.Visible; }
    }
}