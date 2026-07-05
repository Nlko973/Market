using System;
using System.Windows;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class Main : Window
    {
        private readonly ApiClient apiClient = new ApiClient();

        public Main()
        {
            InitializeComponent();
            if (AppState.CurrentStoreId <= 0) { MessageBox.Show("Ошибка контекста магазина. Войдите заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); new MainWindow().Show(); Close(); return; }
            _ = LoadUserProfile();
        }

        private int CurrentStoreId => AppState.CurrentStoreId;

        private async System.Threading.Tasks.Task LoadUserProfile()
        {
            if (string.IsNullOrEmpty(AppState.CurrentUsername))
            {
                return;
            }

            try
            {
                var profile = await apiClient.GetAsync<UserProfileResponse>($"/auth/users/me/{CurrentStoreId}?username={Uri.EscapeDataString(AppState.CurrentUsername)}");
                name.Content = profile.fullName;
                role.Content = $"Роль: {GetRoleDisplayName(profile.role)}";
                AppState.Role = profile.role;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetRoleDisplayName(string role)
        {
            switch (role)
            {
                case "admin":
                    return "Администратор";
                case "cashier":
                    return "Продавец";
                case "owner":
                    return "Владелец";
                default:
                    return role;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AppState.Reset();
            var next = new Market.MainWindow();
            next.Show();
            this.Close();
        }

        private void product_management_btn_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.Role == "owner" || AppState.Role == "admin")
            {
                var productsWindow = new Market.Products();
                productsWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Этот раздел доступен только для Администратора магазина.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void info_btn_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new Market.Info();
            infoWindow.Show();
            this.Close();
        }

        private void conducting_sales_btn_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.Role == "cashier")
            {
                var salesWindow = new Market.Sales();
                salesWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Этот раздел доступен только для Кассира.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}






