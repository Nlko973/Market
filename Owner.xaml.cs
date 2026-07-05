using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class Owner : Window
    {
        private readonly ApiClient apiClient = new ApiClient();

        public Owner()
        {
            InitializeComponent();
            if (AppState.CurrentStoreId <= 0) { MessageBox.Show("Ошибка контекста магазина. Войдите заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); new MainWindow().Show(); Close(); return; }
            _ = LoadOwnerInfo();
            _ = LoadUsers();
        }

        private int CurrentStoreId => AppState.CurrentStoreId;

        private string SelectedRole => btn_seller.IsChecked == true ? "cashier" : "admin";

        private async System.Threading.Tasks.Task LoadOwnerInfo()
        {
            try
            {
                var profile = await apiClient.GetAsync<UserProfileResponse>($"/auth/users/me/{CurrentStoreId}?username={Uri.EscapeDataString(AppState.CurrentUsername)}");
                get_store_name.Content = profile.storeName;
                get_user_name.Content = profile.fullName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных владельца: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadUsers()
        {
            if (user_list == null)
            {
                return;
            }

            user_list.Items.Clear();
            try
            {
                var response = await apiClient.GetAsync<EmployeeListResponse>($"/auth/employees/{CurrentStoreId}?role={SelectedRole}");
                var users = new List<string>();
                if (response.items != null)
                {
                    foreach (var item in response.items)
                    {
                        users.Add(item.fullName);
                    }
                }
                users.Sort(StringComparer.InvariantCultureIgnoreCase);
                foreach (var user in users)
                {
                    user_list.Items.Add(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ClearInputFields();
        }

        private void ClearInputFields()
        {
            fio_input.Text = "";
            login_input.Text = "";
            email_input.Text = "";
            pass_input.Password = "";
            pass_confirm_input.Password = "";
            get_fio.Content = "ФИО: ";
            get_email.Content = "Email: ";
            get_login.Content = "Логин: ";
        }

        private async void btn_seller_Checked(object sender, RoutedEventArgs e)
        {
            await LoadUsers();
        }

        private async void btn_administrator_Checked(object sender, RoutedEventArgs e)
        {
            await LoadUsers();
        }

        private async void user_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (user_list.SelectedItem == null)
            {
                get_fio.Content = "ФИО: ";
                get_email.Content = "Email: ";
                get_login.Content = "Логин: ";
                return;
            }

            string selectedUser = user_list.SelectedItem.ToString();
            try
            {
                var response = await apiClient.GetAsync<EmployeeListResponse>($"/auth/employees/{CurrentStoreId}?role={SelectedRole}");
                var item = response.items == null ? null : Array.Find(response.items, x => x.fullName == selectedUser);
                if (item != null)
                {
                    get_fio.Content = "ФИО: " + item.fullName;
                    get_email.Content = "Email: " + item.email;
                    get_login.Content = "Логин: " + item.username;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            fio_input.Clear();
            login_input.Clear();
            email_input.Clear();
            pass_input.Clear();
            pass_confirm_input.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AppState.Reset();
            var login = new Market.MainWindow();
            login.Show();
            this.Close();
        }

        private async void add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!InputValidator.TryNormalizeFullName(fio_input.Text, out string fio, out string error) ||
                !InputValidator.TryNormalizeUsername(login_input.Text, out string username, out error) ||
                !InputValidator.TryNormalizeEmail(email_input.Text, out string email, out error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string password = pass_input.Password;
            string passwordConfirm = pass_confirm_input.Password;

            if (!InputValidator.TryValidatePasswordPair(password, passwordConfirm, out error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            fio_input.Text = fio;
            login_input.Text = username;
            email_input.Text = email;

            AppState.PendingEmployeeStoreId = CurrentStoreId;
            AppState.PendingEmployeeFullName = fio;
            AppState.PendingEmployeeUsername = username;
            AppState.PendingEmployeeEmail = email;
            AppState.PendingEmployeePassword = password;
            AppState.PendingEmployeeRole = SelectedRole;

            if (!await TryPrepareCaptcha())
            {
                return;
            }

            var confirm = ConfirmationRegistration.ForEmployee(this);
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
                    MessageBox.Show("Капча не подтверждена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                AppState.PendingRegistrationCaptchaId = captcha.captchaId;
                AppState.PendingRegistrationCaptchaAnswer = answer;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка капчи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async System.Threading.Tasks.Task CompleteEmployeeRegistration()
        {
            await LoadUsers();
            MessageBox.Show("Пользователь успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void remove_btn_Click(object sender, RoutedEventArgs e)
        {
            if (user_list.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedUser = user_list.SelectedItem.ToString();
            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/employees/delete", new
                {
                    storeId = CurrentStoreId,
                    fullName = selectedUser,
                });
                await LoadUsers();
                MessageBox.Show("Пользователь успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void fio_input_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void login_input_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = InputValidator.NormalizeUsername(login_input.Text);
            if (login_input.Text != text)
            {
                login_input.Text = text;
                login_input.SelectionStart = login_input.Text.Length;
            }
        }
    }
}






