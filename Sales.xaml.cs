using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Market.Models;
using Market.Services;

namespace Market
{
    public partial class Sales : Window
    {
        private readonly ApiClient apiClient = new ApiClient();
        private readonly List<CartItem> cartItems = new List<CartItem>();
        private readonly List<SaleProductItem> allProducts = new List<SaleProductItem>();

        private class CartItem
        {
            public int ProductID { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }

            public override string ToString() => $"{Quantity}. {Name} - {Price:F2}R";
        }

        public Sales()
        {
            InitializeComponent();
            if (AppState.CurrentStoreId <= 0) { MessageBox.Show("Ошибка контекста магазина. Войдите заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); new MainWindow().Show(); Close(); return; }
            _ = LoadProducts();
            UpdateCartDisplay();
        }

        private int CurrentStoreId => AppState.CurrentStoreId;

        private async System.Threading.Tasks.Task LoadProducts(string searchQuery = "")
        {
            try
            {
                var response = await apiClient.GetAsync<SaleProductListResponse>($"/auth/sales/products/{CurrentStoreId}");
                allProducts.Clear();
                if (response.items != null)
                {
                    allProducts.AddRange(response.items);
                }
                UpdateProductList(searchQuery);
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
        }

        private void UpdateCartDisplay()
        {
            shopping_cart.Items.Clear();
            decimal totalAmount = 0;

            foreach (var item in cartItems)
            {
                shopping_cart.Items.Add(item);
                totalAmount += item.Price * item.Quantity;
            }

            output_amount.Content = $"{totalAmount:F2}R";
        }

        private void Search_btn_Click(object sender, RoutedEventArgs e)
        {
            string query = InputValidator.NormalizeSpaces(search.Text);
            search.Text = query;
            UpdateProductList(query);
        }

        private void Clear_btn_Click(object sender, RoutedEventArgs e)
        {
            search.Text = "";
            UpdateProductList(string.Empty);
        }

        private void Products_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            product_count.Text = "1";
        }

        private void Reduce_count_btn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(product_count.Text, out int quantity) && quantity > 1)
            {
                product_count.Text = (quantity - 1).ToString();
            }
        }

        private void Increase_count_btn_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(product_count.Text, out int quantity))
            {
                product_count.Text = (quantity + 1).ToString();
            }
        }

        private void Add_product_btn_Click(object sender, RoutedEventArgs e)
        {
            if (products_list.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для добавления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!InputValidator.TryParsePositiveInt(product_count.Text, "количество товара", out int quantity, out string error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            product_count.Text = quantity.ToString();

            string selectedProductName = products_list.SelectedItem.ToString();
            var product = allProducts.Find(x => x.name == selectedProductName);
            if (product == null)
            {
                MessageBox.Show("Товар не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (quantity > product.quantity)
            {
                MessageBox.Show($"Нельзя добавить товар '{product.name}'. Доступно на складе: {product.quantity}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var existingItem = cartItems.Find(item => item.ProductID == product.productId);
            if (existingItem != null)
            {
                if (existingItem.Quantity + quantity > product.quantity)
                {
                    MessageBox.Show($"Нельзя добавить товар '{product.name}'. Доступно на складе: {product.quantity}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                existingItem.Quantity += quantity;
            }
            else
            {
                cartItems.Add(new CartItem
                {
                    ProductID = product.productId,
                    Name = product.name,
                    Price = product.price,
                    Quantity = quantity,
                });
            }

            product_count.Text = "1";
            UpdateCartDisplay();
        }

        private void Remove_product_btn_Click(object sender, RoutedEventArgs e)
        {
            if (shopping_cart.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для удаления из корзины.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!InputValidator.TryParsePositiveInt(product_count.Text, "количество товара", out int quantity, out string error))
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            product_count.Text = quantity.ToString();

            var selectedItem = (CartItem)shopping_cart.SelectedItem;
            if (quantity >= selectedItem.Quantity)
            {
                cartItems.Remove(selectedItem);
            }
            else
            {
                selectedItem.Quantity -= quantity;
            }

            product_count.Text = "1";
            UpdateCartDisplay();
        }

        private async void Make_purchase_btn_Click(object sender, RoutedEventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста. Добавьте товары для оформления покупки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var response = await apiClient.PostAsync<CheckoutResponse>("/auth/sales/checkout", new
                {
                    storeId = CurrentStoreId,
                    items = cartItems.Select(x => new { productId = x.ProductID, quantity = x.Quantity }).ToArray(),
                });

                string message = "Покупка успешно оформлена.";
                if (printing_receipt.IsChecked == true)
                {
                    message += " Чек с номером " + response.receiptId + " готов к печати.";
                    printing_receipt.IsChecked = false;
                }
                if (electronic_receipt.IsChecked == true)
                {
                    message += " Электронный чек будет отправлен.";
                    electronic_receipt.IsChecked = false;
                }

                MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                cartItems.Clear();
                UpdateCartDisplay();
                await LoadProducts();
                product_count.Text = "1";
                search.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении покупки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void printing_receipt_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void electronic_receipt_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void exit_btn_Click(object sender, RoutedEventArgs e)
        {
            var main = new Market.Main();
            main.Show();
            this.Close();
        }
    }
}



