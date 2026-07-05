using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Market.Services
{
    public static class InputValidator
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        private static readonly Regex UsernameRegex = new Regex(@"^[a-z0-9_]{3,32}$", RegexOptions.IgnoreCase);
        private static readonly Regex WordRegex = new Regex(@"^[A-Za-zА-Яа-яЁё]+(?:-[A-Za-zА-Яа-яЁё]+)?$");

        public static string NormalizeSpaces(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
        }

        public static string NormalizeEmail(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        public static string NormalizeUsername(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        public static bool TryNormalizeFullName(string value, out string fullName, out string error)
        {
            fullName = NormalizeSpaces(value);
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                error = "Введите ФИО.";
                return false;
            }

            string[] parts = fullName.Split(' ');
            if (parts.Length != 3)
            {
                error = "ФИО должно состоять из трех слов: фамилия, имя и отчество.";
                return false;
            }

            foreach (string part in parts)
            {
                if (!WordRegex.IsMatch(part))
                {
                    error = "ФИО должно содержать только буквы. Допускается дефис в составных именах.";
                    return false;
                }
            }

            return true;
        }

        public static bool TryNormalizeStoreName(string value, out string storeName, out string error)
        {
            storeName = NormalizeSpaces(value);
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(storeName))
            {
                error = "Введите название магазина.";
                return false;
            }
            if (storeName.Length < 2 || storeName.Length > 100)
            {
                error = "Название магазина должно быть от 2 до 100 символов.";
                return false;
            }

            return true;
        }

        public static bool TryNormalizeUsername(string value, out string username, out string error)
        {
            username = NormalizeUsername(value);
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                error = "Введите логин.";
                return false;
            }
            if (!UsernameRegex.IsMatch(username))
            {
                error = "Логин должен содержать 3-32 символа: латинские буквы, цифры или нижнее подчеркивание.";
                return false;
            }

            return true;
        }

        public static bool TryNormalizeEmail(string value, out string email, out string error)
        {
            email = NormalizeEmail(value);
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                error = "Введите email.";
                return false;
            }
            if (!EmailRegex.IsMatch(email))
            {
                error = "Введите корректный email, например user@example.ru.";
                return false;
            }

            return true;
        }

        public static bool TryValidateLoginOrEmail(string value, out string loginOrEmail, out string error)
        {
            loginOrEmail = NormalizeSpaces(value);
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(loginOrEmail))
            {
                error = "Введите логин или email.";
                return false;
            }

            if (loginOrEmail.Contains("@"))
            {
                return TryNormalizeEmail(loginOrEmail, out loginOrEmail, out error);
            }

            return TryNormalizeUsername(loginOrEmail, out loginOrEmail, out error);
        }

        public static bool TryValidatePassword(string password, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                error = "Введите пароль.";
                return false;
            }
            if (password.Length < 6)
            {
                error = "Пароль должен быть не короче 6 символов.";
                return false;
            }
            if (Regex.IsMatch(password, @"\s"))
            {
                error = "Пароль не должен содержать пробелы.";
                return false;
            }

            return true;
        }

        public static bool TryValidatePasswordPair(string password, string passwordConfirm, out string error)
        {
            if (!TryValidatePassword(password, out error))
            {
                return false;
            }
            if (string.IsNullOrEmpty(passwordConfirm))
            {
                error = "Подтвердите пароль.";
                return false;
            }
            if (password != passwordConfirm)
            {
                error = "Пароли не совпадают.";
                return false;
            }

            return true;
        }

        public static bool TryValidateVerificationCode(string value, out string code, out string error)
        {
            code = (value ?? string.Empty).Trim();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(code))
            {
                error = "Введите код подтверждения.";
                return false;
            }
            if (!Regex.IsMatch(code, @"^\d{6}$"))
            {
                error = "Код подтверждения должен состоять из 6 цифр.";
                return false;
            }

            return true;
        }

        public static bool TryNormalizeName(string value, string fieldName, out string normalized, out string error)
        {
            normalized = NormalizeSpaces(value);
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = $"Введите {fieldName}.";
                return false;
            }
            if (normalized.Length < 2 || normalized.Length > 100)
            {
                error = $"{fieldName} должно быть от 2 до 100 символов.";
                return false;
            }

            return true;
        }

        public static bool TryParseNonNegativeInt(string value, string fieldName, out int number, out string error)
        {
            number = 0;
            error = string.Empty;
            string text = (value ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                error = $"Введите {fieldName}.";
                return false;
            }
            if (!int.TryParse(text, out number) || number < 0)
            {
                error = $"{fieldName} должно быть неотрицательным целым числом.";
                return false;
            }

            return true;
        }

        public static bool TryParsePositiveInt(string value, string fieldName, out int number, out string error)
        {
            if (!TryParseNonNegativeInt(value, fieldName, out number, out error))
            {
                return false;
            }
            if (number <= 0)
            {
                error = $"{fieldName} должно быть больше нуля.";
                return false;
            }

            return true;
        }

        public static bool TryParsePositiveDecimal(string value, string fieldName, out decimal number, out string error)
        {
            number = 0;
            error = string.Empty;
            string text = (value ?? string.Empty).Trim().Replace(',', '.');

            if (string.IsNullOrWhiteSpace(text))
            {
                error = $"Введите {fieldName}.";
                return false;
            }
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number) || number <= 0)
            {
                error = $"{fieldName} должно быть положительным числом.";
                return false;
            }

            return true;
        }
    }
}
