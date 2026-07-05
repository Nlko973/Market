using System;
using System.Windows;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class ForgotPassword : Window
    {
        private readonly ApiClient apiClient = new ApiClient();

        public ForgotPassword()
        {
            InitializeComponent();
        }

        private async void reset_password_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!InputValidator.TryValidateLoginOrEmail(login_or_email_input.Text, out string input, out string error))
            {
                status_message.Foreground = System.Windows.Media.Brushes.Red;
                status_message.Text = error;
                return;
            }
            login_or_email_input.Text = input;

            try
            {
                var captcha = await apiClient.GetAsync<CaptchaResponse>("/auth/captcha");
                string answer = Microsoft.VisualBasic.Interaction.InputBox($"Капча: {captcha.question}", "Проверка", "").Trim();

                if (string.IsNullOrWhiteSpace(answer))
                {
                    status_message.Foreground = System.Windows.Media.Brushes.Red;
                    status_message.Text = "Капча не подтверждена.";
                    return;
                }

                await apiClient.PostAsync<ApiMessage>("/auth/send-code", new
                {
                    email = input,
                    purpose = "resetPassword",
                    captchaId = captcha.captchaId,
                    captchaAnswer = answer,
                });

                AppState.RecoveryEmail = input;
                status_message.Foreground = System.Windows.Media.Brushes.Green;
                status_message.Text = "Код отправлен на email (10 минут).";
            }
            catch (Exception ex)
            {
                status_message.Foreground = System.Windows.Media.Brushes.Red;
                status_message.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void close_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenMainWindow();
        }

        private void OpenMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void enter_code_btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AppState.RecoveryEmail))
            {
                message.Content = "Сначала отправьте код на почту.";
                return;
            }
            if (!InputValidator.TryValidateVerificationCode(code_input.Text, out string code, out string error))
            {
                message.Content = error;
                return;
            }

            code_input.Text = code;
            AppState.RecoveryCode = code;
            var changePassword = new ResetPassword();
            changePassword.Show();
            this.Close();
        }
    }
}
