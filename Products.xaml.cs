using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class Products : Window
    {
        private readonly ApiClient apiClient = new ApiClient();
        private readonly List<ProductItem> allProducts = new List<ProductItem>();
        private readonly List<string> allCategories = new List<string>();

        public Products()
        {
            InitializeComponent();
            if (AppState.CurrentStoreId <= 0) { MessageBox.Show("Ошибка контекста магазина. Войдите заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); new MainWindow().Show(); Close(); return; }
            SetFieldsReadOnly(true);
            allow_changes.IsEnabled = false;
            _ = LoadInitialData();
        }

        private int CurrentStoreId => AppState.CurrentStoreId;

        private async System.Threading.Tasks.Task LoadInitialData()
        {
            await LoadCategories();
            await LoadAllProducts();
        }

        private async System.Threading.Tasks.Task LoadCategories()
        {
            try
            {
                var response = await apiClient.GetAsync<CategoryListResponse>($"/auth/categories/{CurrentStoreId}");
                allCategories.Clear();
                input_product_category.Items.Clear();
                product_category.Items.Clear();
                if (response.items != null)
                {
                    foreach (var category in response.items)
                    {
                        allCategories.Add(category.name);
                    }
                }
                allCategories.Sort(StringComparer.InvariantCultureIgnoreCase);
                foreach (var category in allCategories)
                {
                    input_product_category.Items.Add(category);
                    product_category.Items.Add(category);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadAllProducts()
        {
            try
            {
                var response = await apiClient.GetAsync<ProductListResponse>($"/auth/products/{CurrentStoreId}");
                allProducts.Clear();
                if (response.items != null)
                {
                    allProducts.AddRange(response.items);
                }
                UpdateProductList(string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProductList(string searchQuery)
        {
            products_list.Items.Clear();
            foreach (var product in allProducts)
            {
                if (string.IsNullOrEmpty(searchQuery) || product.name.ToLower().Contains(searchQuery.ToLower()))
                {
                    products_list.Items.Add(product.name);
                }
            }
            ClearDisplayFields();
        }

        private void ClearDisplayFields()
        {
            product_name.Text = "";
            product_category.SelectedIndex = -1;
            product_barcode.Text = "";
            product_count.Text = "";
            product_price.Text = "";
        }

        private void ClearInputFields()
        {
            input_product_name.Text = "";
            input_product_category.SelectedIndex = -1;
            input_product_barcode.Text = "";
            input_product_count.Text = "";
            input_product_price.Text = "";
        }

        private void SetFieldsReadOnly(bool readOnly)
        {
            product_name.IsReadOnly = readOnly;
            product_category.IsEnabled = !readOnly;
            product_barcode.IsReadOnly = readOnly;
            product_count.IsReadOnly = readOnly;
            product_price.IsReadOnly = readOnly;
            save_changes_btn.IsEnabled = !readOnly;
        }

        private void search_btn_Click(object sender, RoutedEventArgs e)
        {
            string query = InputValidator.NormalizeSpaces(search.Text);
            search.Text = query;
            UpdateProductList(query);
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            search.Text = "";
            UpdateProductList(string.Empty);
        }

        private void products_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (products_list.SelectedItem == null)
            {
                allow_changes.IsEnabled = false;
                allow_changes.IsChecked = false;
                return;
            }

            string selectedName = products_list.SelectedItem.ToString();
            var selectedProduct = allProducts.Find(x => x.name == selectedName);
            if (selectedProduct == null)
            {
                return;
            }

            product_name.Text = selectedProduct.name;
            product_category.SelectedItem = selectedProduct.category;
            product_barcode.Text = selectedProduct.barcode;
            product_count.Text = selectedProduct.quantity.ToString();
            product_price.Text = selectedProduct.price.ToString();

            SetFieldsReadOnly(true);
            allow_changes.IsEnabled = true;
            allow_changes.IsChecked = false;
        }

        private async void add_product_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadProductFields(input_product_name.Text, GetSelectedCategory(input_product_category), input_product_barcode.Text, input_product_count.Text, input_product_price.Text, out var productData, out var error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            input_product_name.Text = productData.name;
            input_product_barcode.Text = productData.barcode;
            input_product_count.Text = productData.quantity.ToString();
            input_product_price.Text = productData.price.ToString("0.##");

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/products/upsert", new
                {
                    storeId = CurrentStoreId,
                    originalName = (string)null,
                    name = productData.name,
                    category = productData.category,
                    barcode = productData.barcode,
                    quantity = productData.quantity,
                    price = productData.price,
                });
                await LoadAllProducts();
                ClearInputFields();
                MessageBox.Show("Товар успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void remove_product_btn_Click(object sender, RoutedEventArgs e)
        {
            if (products_list.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/products/delete", new
                {
                    storeId = CurrentStoreId,
                    name = products_list.SelectedItem.ToString(),
                });
                await LoadAllProducts();
                ClearDisplayFields();
                MessageBox.Show("Товар успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void save_changes_btn_Click(object sender, RoutedEventArgs e)
        {
            if (products_list.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!TryReadProductFields(product_name.Text, GetSelectedCategory(product_category), product_barcode.Text, product_count.Text, product_price.Text, out var productData, out var error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            product_name.Text = productData.name;
            product_barcode.Text = productData.barcode;
            product_count.Text = productData.quantity.ToString();
            product_price.Text = productData.price.ToString("0.##");

            try
            {
                await apiClient.PostAsync<ApiMessage>("/auth/products/upsert", new
                {
                    storeId = CurrentStoreId,
                    originalName = products_list.SelectedItem.ToString(),
                    name = productData.name,
                    category = productData.category,
                    barcode = productData.barcode,
                    quantity = productData.quantity,
                    price = productData.price,
                });
                await LoadAllProducts();
                allow_changes.IsChecked = false;
                MessageBox.Show("Изменения успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изменений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TryReadProductFields(string name, string category, string barcode, string quantityText, string priceText, out (string name, string category, string barcode, int quantity, decimal price) productData, out string error)
        {
            productData = default;
            error = string.Empty;

            if (!InputValidator.TryNormalizeName(name, "название товара", out name, out error))
            {
                return false;
            }
            if (!InputValidator.TryNormalizeName(category, "категорию товара", out category, out error))
            {
                return false;
            }

            barcode = (barcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barcode))
            {
                error = "Введите штрих-код.";
                return false;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(barcode, @"^\d{4,64}$"))
            {
                error = "Штрих-код должен содержать только цифры, от 4 до 64 символов.";
                return false;
            }
            if (!InputValidator.TryParseNonNegativeInt(quantityText, "количество", out int quantity, out error))
            {
                return false;
            }
            if (!InputValidator.TryParsePositiveDecimal(priceText, "цена", out decimal price, out error))
            {
                return false;
            }

            productData = (name, category, barcode, quantity, price);
            return true;
        }

        private string GetSelectedCategory(ComboBox comboBox)
        {
            return comboBox.SelectedItem == null ? "" : comboBox.SelectedItem.ToString();
        }

        private void allow_changes_Checked(object sender, RoutedEventArgs e)
        {
            SetFieldsReadOnly(false);
        }

        private void allow_changes_Unchecked(object sender, RoutedEventArgs e)
        {
            SetFieldsReadOnly(true);
        }

        private void exit_btn_Click(object sender, RoutedEventArgs e)
        {
            var main = new Market.Main();
            main.Show();
            this.Close();
        }

        private void category_clear_btn_Click(object sender, RoutedEventArgs e)
        {
            var categories = new Categories();
            categories.Show();
            this.Close();
        }
    }
}



