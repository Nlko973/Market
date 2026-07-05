using System;
using System.Windows;
using System.Windows.Controls;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class Categories : Window
    {
        private readonly ApiClient apiClient = new ApiClient();

        public Categories()
        {
            InitializeComponent();
            if (AppState.CurrentStoreId <= 0)
            {
                MessageBox.Show("Ошибка контекста магазина. Войдите заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                new MainWindow().Show();
                Close();
                return;
            }
            _ = LoadCategories();
        }

        private int CurrentStoreId => AppState.CurrentStoreId;

        private async System.Threading.Tasks.Task LoadCategories()
        {
            try
            {
                categories_list.Items.Clear();
                var response = await apiClient.GetAsync<CategoryListResponse>($"/auth/categories/{CurrentStoreId}");
                if (response.items != null)
                {
                    foreach (var category in response.items)
                    {
                        categories_list.Items.Add(category.name);
                    }
                }

                ClearSelection();
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка загрузки категорий: " + ex.Message;
            }
        }

        private void ClearSelection()
        {
            category_name_input.Text = "";
            save_btn.IsEnabled = false;
            delete_btn.IsEnabled = false;
        }

        private void categories_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (categories_list.SelectedItem == null)
            {
                ClearSelection();
                return;
            }

            category_name_input.Text = categories_list.SelectedItem.ToString();
            save_btn.IsEnabled = true;
            delete_btn.IsEnabled = true;
        }

        private async void add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadCategoryName(out string name))
            {
                return;
            }

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/categories/upsert", new
                {
                    storeId = CurrentStoreId,
                    originalName = (string)null,
                    name,
                });
                message.Content = "Категория добавлена.";
                await LoadCategories();
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка добавления категории: " + ex.Message;
            }
        }

        private async void save_btn_Click(object sender, RoutedEventArgs e)
        {
            if (categories_list.SelectedItem == null)
            {
                message.Content = "Выберите категорию.";
                return;
            }

            if (!TryReadCategoryName(out string name))
            {
                return;
            }

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/categories/upsert", new
                {
                    storeId = CurrentStoreId,
                    originalName = categories_list.SelectedItem.ToString(),
                    name,
                });
                message.Content = "Категория обновлена.";
                await LoadCategories();
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка обновления категории: " + ex.Message;
            }
        }

        private bool TryReadCategoryName(out string name)
        {
            if (!InputValidator.TryNormalizeName(category_name_input.Text, "название категории", out name, out string error))
            {
                message.Content = error;
                return false;
            }

            category_name_input.Text = name;
            return true;
        }

        private async void delete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (categories_list.SelectedItem == null)
            {
                message.Content = "Выберите категорию.";
                return;
            }

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/categories/delete", new
                {
                    storeId = CurrentStoreId,
                    name = categories_list.SelectedItem.ToString(),
                });
                message.Content = "Категория удалена.";
                await LoadCategories();
            }
            catch (Exception ex)
            {
                message.Content = "Ошибка удаления категории: " + ex.Message;
            }
        }

        private void exit_btn_Click(object sender, RoutedEventArgs e)
        {
            var back = new Products();
            back.Show();
            Close();
        }
    }
}
