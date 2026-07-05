using System;
using System.Windows;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class ResetPassword : Window
    {
        private readonly ApiClient apiClient = new ApiClient();

        public ResetPassword()
        {
            InitializeComponent();
        }

        private async void change_password_btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AppState.RecoveryEmail) || string.IsNullOrEmpty(AppState.RecoveryCode))
            {
                message.Content = "Не найден логин/email или код восстановления. Начните заново.";
                return;
            }

            string password = pass_input.Password;
            string passwordConfirm = pass_confirm_input.Password;
            if (!InputValidator.TryValidatePasswordPair(password, passwordConfirm, out string error))
            {
                message.Content = error;
                return;
            }

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/reset-password", new
                {
                    email = AppState.RecoveryEmail,
                    code = AppState.RecoveryCode,
                    newPassword = password,
                });

                MessageBox.Show("Пароль успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                AppState.RecoveryEmail = null;
                AppState.RecoveryCode = null;

                var back = new MainWindow();
                back.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка: " + ex.Message;
            }
        }

        private void close_window_btn_Click(object sender, RoutedEventArgs e)
        {
            var back = new ForgotPassword();
            back.Show();
            this.Close();
        }
    }
}
