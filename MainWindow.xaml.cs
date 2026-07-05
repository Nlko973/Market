using System;
using System.Windows;
using System.Windows.Controls;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class MainWindow : Window
    {
        private readonly ApiClient apiClient = new ApiClient();
        private bool isLoginInProgress;

        public MainWindow()
        {
            InitializeComponent();

            var session = SessionStorage.Load();
            if (session != null)
            {
                AppState.CurrentUsername = session.Username;
                AppState.Role = session.Role;
                AppState.CurrentStoreId = session.StoreId;
                AppState.AccessToken = session.AccessToken;
                AppState.IsAuthenticated = true;

                Window next = session.Role == "owner" ? (Window)new Market.Owner() : new Market.Main();
                next.Show();
                Close();
            }
        }

        private async void enter_btn_Click(object sender, RoutedEventArgs e)
        {
            if (isLoginInProgress)
            {
                return;
            }

            ShowLoginError("");

            if (!InputValidator.TryValidateLoginOrEmail(login.Text, out string username, out string error))
            {
                ShowLoginError(error);
                return;
            }

            string userPassword = password.Password;
            if (!InputValidator.TryValidatePassword(userPassword, out error))
            {
                ShowLoginError(error);
                return;
            }

            login.Text = username;

            try
            {
                SetLoginInProgress(true);
                var token = await apiClient.PostAsync<TokenResponse>("/auth/login", new
                {
                    username,
                    password = userPassword,
                });

                AppState.CurrentUsername = string.IsNullOrEmpty(token.username) ? username : token.username;
                AppState.Role = token.role;
                AppState.IsAuthenticated = true;
                AppState.CurrentStoreId = token.storeId;
                AppState.AccessToken = token.accessToken;

                SessionStorage.Save(new SessionState
                {
                    AccessToken = token.accessToken,
                    Username = AppState.CurrentUsername,
                    Role = token.role,
                    StoreId = token.storeId,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
                });

                Window next = token.role == "owner" ? (Window)new Market.Owner() : new Market.Main();
                next.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowLoginError(GetLoginErrorMessage(ex));
            }
            finally
            {
                SetLoginInProgress(false);
            }
        }

        private void SetLoginInProgress(bool value)
        {
            isLoginInProgress = value;
            enter_btn.IsEnabled = !value;
            enter_btn.Content = value ? "Вход..." : "Войти";
            login.IsEnabled = !value;
            password.IsEnabled = !value;
        }

        private void ShowLoginError(string message)
        {
            login_error.Text = message;
        }

        private string GetLoginErrorMessage(Exception ex)
        {
            if (ex.Message.Contains("401") || ex.Message.Contains("Неверный логин или пароль"))
            {
                return "Неверный логин или пароль.";
            }

            if (ex.Message.Contains("Не удалось подключиться к backend"))
            {
                return "Не удалось подключиться к серверу. Проверьте интернет и доступность backend.";
            }

            return ex.Message;
        }

        private void forgot_password_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var forgotPasswordWindow = new ForgotPassword();
            forgotPasswordWindow.Show();
            this.Close();
        }

        private void registration_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var registrationWindow = new Registration();
            registrationWindow.Show();
            this.Close();
        }

        private void login_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }

    public static class AppState
    {
        public static string CurrentUsername { get; set; }
        public static string Role { get; set; }
        public static bool IsAuthenticated { get; set; }
        public static long? CurrentShiftID { get; set; }
        public static int CurrentStoreId { get; set; }
        public static string AccessToken { get; set; }

        public static string RecoveryEmail { get; set; }
        public static string RecoveryCode { get; set; }

        public static string PendingStoreName { get; set; }
        public static string PendingOwnerEmail { get; set; }
        public static string PendingOwnerFullName { get; set; }
        public static string PendingOwnerUsername { get; set; }
        public static string PendingOwnerPassword { get; set; }
        public static int PendingEmployeeStoreId { get; set; }
        public static string PendingEmployeeEmail { get; set; }
        public static string PendingEmployeeFullName { get; set; }
        public static string PendingEmployeeUsername { get; set; }
        public static string PendingEmployeePassword { get; set; }
        public static string PendingEmployeeRole { get; set; }
        public static string PendingRegistrationCaptchaId { get; set; }
        public static string PendingRegistrationCaptchaAnswer { get; set; }

        public static void Reset()
        {
            CurrentUsername = null;
            Role = null;
            IsAuthenticated = false;
            CurrentShiftID = null;
            CurrentStoreId = 0;
            AccessToken = null;

            RecoveryEmail = null;
            RecoveryCode = null;
            PendingStoreName = null;
            PendingOwnerEmail = null;
            PendingOwnerFullName = null;
            PendingOwnerUsername = null;
            PendingOwnerPassword = null;
            PendingEmployeeStoreId = 0;
            PendingEmployeeEmail = null;
            PendingEmployeeFullName = null;
            PendingEmployeeUsername = null;
            PendingEmployeePassword = null;
            PendingEmployeeRole = null;
            PendingRegistrationCaptchaId = null;
            PendingRegistrationCaptchaAnswer = null;

            SessionStorage.Clear();
        }
    }
}
