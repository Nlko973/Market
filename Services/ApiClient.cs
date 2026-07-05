using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Market.Services
{
    public class ApiClient
    {
        private static readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private const int RequestTimeoutMs = 10000;

        private static bool IsProtected(string endpoint)
        {
            return !(endpoint == "/auth/login" || endpoint == "/auth/register-store" || endpoint == "/auth/send-code" || endpoint == "/auth/captcha" || endpoint == "/auth/reset-password" || endpoint == "/auth/verify-code");
        }

        private static void ApplyAuthHeader(HttpWebRequest request, string endpoint)
        {
            if (IsProtected(endpoint) && !string.IsNullOrEmpty(AppState.AccessToken))
            {
                request.Headers[HttpRequestHeader.Authorization] = $"Bearer {AppState.AccessToken}";
            }
        }

        public async Task<TResponse> PostAsync<TResponse>(string endpoint, object payload)
        {
            var url = $"{BackendSettings.BaseUrl}{endpoint}";
            var body = await SendAsync("POST", url, endpoint, serializer.Serialize(payload));
            return serializer.Deserialize<TResponse>(body);
        }

        public async Task<TResponse> GetAsync<TResponse>(string endpoint)
        {
            var url = $"{BackendSettings.BaseUrl}{endpoint}";
            var body = await SendAsync("GET", url, endpoint, null);
            return serializer.Deserialize<TResponse>(body);
        }

        private static async Task<string> SendAsync(string method, string url, string endpoint, string json)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Timeout = RequestTimeoutMs;
            request.ReadWriteTimeout = RequestTimeoutMs;
            request.KeepAlive = false;
            request.Proxy = null;
            ApplyAuthHeader(request, endpoint);

            if (json != null)
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = bytes.Length;
                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }

            try
            {
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    return await ReadResponseAsync(response);
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response != null)
                {
                    using (response)
                    {
                        var errorBody = await ReadResponseAsync(response);
                        throw new WebException(FormatServerError((int)response.StatusCode, errorBody), ex);
                    }
                }

                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new WebException($"Превышено время ожидания ответа от backend: {url}", ex);
                }

                throw new WebException(FormatConnectionError(url, ex), ex);
            }
        }

        private static async Task<string> ReadResponseAsync(HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static string FormatConnectionError(string url, Exception ex)
        {
            return $"Не удалось подключиться к backend: {url}. {GetDeepestMessage(ex)}";
        }

        private static string GetDeepestMessage(Exception ex)
        {
            var current = ex;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        private static string FormatServerError(int statusCode, string body)
        {
            string title = GetStatusTitle(statusCode);
            string detail = ExtractDetail(body);
            return string.IsNullOrWhiteSpace(detail)
                ? $"{statusCode}: {title}"
                : $"{statusCode}: {title}. {detail}";
        }

        private static string GetStatusTitle(int statusCode)
        {
            switch (statusCode)
            {
                case 400:
                    return "ошибка запроса";
                case 401:
                    return "ошибка авторизации";
                case 403:
                    return "доступ запрещён";
                case 404:
                    return "не найдено";
                case 409:
                    return "конфликт данных";
                case 422:
                    return "ошибка валидации";
                case 500:
                    return "ошибка сервера";
                default:
                    return "ошибка сервера";
            }
        }

        private static string ExtractDetail(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return "";
            }

            try
            {
                var parsed = serializer.DeserializeObject(body) as IDictionary;
                if (parsed == null || !parsed.Contains("detail"))
                {
                    return body;
                }

                return FormatDetail(parsed["detail"]);
            }
            catch
            {
                return body;
            }
        }

        private static string FormatDetail(object detail)
        {
            if (detail == null)
            {
                return "";
            }

            if (detail is string text)
            {
                return TranslateDetail(text);
            }

            if (detail is ArrayList list)
            {
                var messages = new StringBuilder();
                foreach (var item in list)
                {
                    if (messages.Length > 0)
                    {
                        messages.Append("; ");
                    }

                    messages.Append(FormatValidationItem(item));
                }

                return messages.ToString();
            }

            return detail.ToString();
        }

        private static string FormatValidationItem(object item)
        {
            var dict = item as IDictionary;
            if (dict == null)
            {
                return item.ToString();
            }

            string field = "";
            if (dict.Contains("loc") && dict["loc"] is ArrayList loc)
            {
                field = string.Join(".", loc.ToArray());
            }

            string message = dict.Contains("msg") ? dict["msg"].ToString() : "некорректное значение";
            return string.IsNullOrEmpty(field) ? message : $"{field}: {message}";
        }

        private static string TranslateDetail(string detail)
        {
            switch (detail)
            {
                case "Missing bearer token":
                case "Invalid token":
                case "Invalid token payload":
                case "User not found":
                    return "Войдите в систему заново.";
                case "Invalid credentials":
                    return "Неверный логин или пароль.";
                case "Forbidden for this store":
                    return "Нет доступа к этому магазину.";
                case "Invalid captcha":
                    return "Неверная капча.";
                case "Invalid or expired code":
                case "Invalid owner verification code":
                case "Invalid employee verification code":
                    return "Неверный или просроченный код подтверждения.";
                case "Store already exists":
                    return "Магазин уже существует.";
                case "Username already exists":
                    return "Логин уже используется.";
                case "Email already exists":
                    return "Email уже используется.";
                case "Full name already exists":
                    return "Пользователь с таким ФИО уже существует.";
                case "User with this login or email not found":
                    return "Пользователь с таким логином или email не найден.";
                case "Login or email is not unique across stores":
                    return "Логин или email найден в нескольких магазинах. Используйте вход через конкретный магазин.";
                case "Invalid email":
                    return "Введите корректный email.";
                case "Category name is required":
                    return "Введите название категории.";
                case "Category already exists":
                    return "Категория уже существует.";
                case "Category not found":
                    return "Категория не найдена.";
                case "Category has products and cannot be deleted":
                    return "Нельзя удалить категорию, к которой привязаны товары.";
                case "Product with this barcode already exists":
                    return "Товар с таким штрих-кодом уже существует.";
                case "Product already exists":
                    return "Товар уже существует.";
                case "Product not found":
                    return "Товар не найден.";
                case "Product has sales and cannot be deleted":
                    return "Нельзя удалить товар, по которому уже были продажи.";
                default:
                    return detail;
            }
        }
    }
}
