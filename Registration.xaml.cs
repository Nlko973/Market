using System;
using System.Windows;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class Registration : Window
    {
        private readonly ApiClient apiClient = new ApiClient();

        public Registration()
        {
            InitializeComponent();
        }

        private async void registration_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!InputValidator.TryNormalizeStoreName(store_name_input.Text, out string storeName, out string error) ||
                !InputValidator.TryNormalizeFullName(fio_input.Text, out string fullName, out error) ||
                !InputValidator.TryNormalizeUsername(login_input.Text, out string username, out error) ||
                !InputValidator.TryNormalizeEmail(email_input.Text, out string email, out error))
            {
                message.Content = error;
                return;
            }

            string password = pass_input.Password;
            string passwordConfirm = pass_confirm_input.Password;
            if (!InputValidator.TryValidatePasswordPair(password, passwordConfirm, out error))
            {
                message.Content = error;
                return;
            }

            store_name_input.Text = storeName;
            fio_input.Text = fullName;
            login_input.Text = username;
            email_input.Text = email;

            AppState.PendingStoreName = storeName;
            AppState.PendingOwnerFullName = fullName;
            AppState.PendingOwnerUsername = username;
            AppState.PendingOwnerEmail = email;
            AppState.PendingOwnerPassword = password;

            if (!await TryPrepareCaptcha())
            {
                return;
            }

            var confirm = ConfirmationRegistration.ForStore(this);
            confirm.Show();
            Hide();
        }

        private async System.Threading.Tasks.Task<bool> TryPrepareCaptcha()
        {
            try
            {
                var captcha = await apiClient.GetAsync<CaptchaResponse>("/auth/captcha");
                string answer = Microsoft.VisualBasic.Interaction.InputBox($"Капча: {captcha.question}", "Проверка", "");
                if (string.IsNullOrWhiteSpace(answer))
                {
                    message.Content = "Капча не подтверждена.";
                    return false;
                }

                AppState.PendingRegistrationCaptchaId = captcha.captchaId;
                AppState.PendingRegistrationCaptchaAnswer = answer.Trim();
                return true;
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка: " + ex.Message;
                return false;
            }
        }

        private void close_window_btn_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MainWindow();
            menu.Show();
            this.Close();
        }
    }
}
