using System;
using System.Windows;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class ConfirmationRegistration : Window
    {
        private readonly ApiClient apiClient = new ApiClient();
        private readonly Window previousWindow;
        private readonly RegistrationMode mode;
        private bool codeSent;

        private enum RegistrationMode
        {
            Store,
            Employee,
        }

        private ConfirmationRegistration(RegistrationMode mode, Window previousWindow)
        {
            InitializeComponent();
            this.mode = mode;
            this.previousWindow = previousWindow;
            enter_btn.Content = mode == RegistrationMode.Store ? "Зарегистрировать магазин" : "Зарегистрировать сотрудника";
            Loaded += ConfirmationRegistration_Loaded;
        }

        public static ConfirmationRegistration ForStore(Window previousWindow)
        {
            return new ConfirmationRegistration(RegistrationMode.Store, previousWindow);
        }

        public static ConfirmationRegistration ForEmployee(Window previousWindow)
        {
            return new ConfirmationRegistration(RegistrationMode.Employee, previousWindow);
        }

        private string PendingEmail => mode == RegistrationMode.Store ? AppState.PendingOwnerEmail : AppState.PendingEmployeeEmail;

        private async void ConfirmationRegistration_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PendingEmail))
            {
                message.Content = "Нет email для регистрации.";
                return;
            }
            if (string.IsNullOrEmpty(AppState.PendingRegistrationCaptchaId) || string.IsNullOrWhiteSpace(AppState.PendingRegistrationCaptchaAnswer))
            {
                message.Content = "Капча не подтверждена. Вернитесь назад.";
                return;
            }

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/send-code", new
                {
                    email = PendingEmail,
                    purpose = "register",
                    captchaId = AppState.PendingRegistrationCaptchaId,
                    captchaAnswer = AppState.PendingRegistrationCaptchaAnswer,
                });

                codeSent = true;
                message.Content = "Код отправлен на почту.";
                AppState.PendingRegistrationCaptchaId = null;
                AppState.PendingRegistrationCaptchaAnswer = null;
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка: " + ex.Message;
            }
        }

        private async void enter_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!codeSent)
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

            try
            {
                if (mode == RegistrationMode.Store)
                {
                    await RegisterStore(code);
                }
                else
                {
                    await RegisterEmployee(code);
                }
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка: " + ex.Message;
            }
        }

        private async System.Threading.Tasks.Task RegisterStore(string code)
        {
            if (string.IsNullOrEmpty(AppState.PendingStoreName) || string.IsNullOrEmpty(AppState.PendingOwnerEmail))
            {
                message.Content = "Нет данных регистрации. Начните регистрацию заново.";
                return;
            }

            await apiClient.PostAsync<ApiMessage>("/auth/register-store", new
            {
                storeName = AppState.PendingStoreName,
                ownerEmail = AppState.PendingOwnerEmail,
                ownerFullName = AppState.PendingOwnerFullName,
                ownerUsername = AppState.PendingOwnerUsername,
                ownerPassword = AppState.PendingOwnerPassword,
                ownerVerificationCode = code,
            });

            var token = await apiClient.PostAsync<TokenResponse>("/auth/login", new
            {
                storeName = AppState.PendingStoreName,
                username = AppState.PendingOwnerUsername,
                password = AppState.PendingOwnerPassword,
            });

            AppState.CurrentUsername = AppState.PendingOwnerUsername;
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

            AppState.PendingStoreName = null;
            AppState.PendingOwnerEmail = null;
            AppState.PendingOwnerFullName = null;
            AppState.PendingOwnerUsername = null;
            AppState.PendingOwnerPassword = null;
            AppState.PendingRegistrationCaptchaId = null;
            AppState.PendingRegistrationCaptchaAnswer = null;

            previousWindow?.Close();
            var next = token.role == "owner" ? (Window)new Owner() : new Main();
            next.Show();
            Close();
        }

        private async System.Threading.Tasks.Task RegisterEmployee(string code)
        {
            await apiClient.PostAsync<ApiMessage>("/auth/employees/create", new
            {
                storeId = AppState.PendingEmployeeStoreId,
                fullName = AppState.PendingEmployeeFullName,
                username = AppState.PendingEmployeeUsername,
                email = AppState.PendingEmployeeEmail,
                password = AppState.PendingEmployeePassword,
                role = AppState.PendingEmployeeRole,
                verificationCode = code,
            });

            AppState.PendingEmployeeStoreId = 0;
            AppState.PendingEmployeeEmail = null;
            AppState.PendingEmployeeFullName = null;
            AppState.PendingEmployeeUsername = null;
            AppState.PendingEmployeePassword = null;
            AppState.PendingEmployeeRole = null;
            AppState.PendingRegistrationCaptchaId = null;
            AppState.PendingRegistrationCaptchaAnswer = null;

            if (previousWindow is Owner owner)
            {
                await owner.CompleteEmployeeRegistration();
                owner.Show();
            }

            Close();
        }

        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            previousWindow?.Show();
            Close();
        }
    }
}

