using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// Атрибуты ниже задают основные сведения о сборке: название приложения,
// продукт, организацию, авторские права и другие данные, которые видны
// в свойствах исполняемого файла.
[assembly: AssemblyTitle("Market")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Market")]
[assembly: AssemblyCopyright("Copyright ©  2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Значение false скрывает типы сборки от COM-компонентов. Для WPF-приложения
// Market взаимодействие через COM не используется, поэтому типы не публикуются.
[assembly: ComVisible(false)]

// Если в проекте появится локализация, основную культуру интерфейса можно
// указать через параметр <UICulture> в файле .csproj и включить атрибут
// NeutralResourcesLanguage ниже.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, // Отдельные словари ресурсов для тем не используются.
    ResourceDictionaryLocation.SourceAssembly // Общие ресурсы находятся в текущей сборке.
)]


// Версия сборки состоит из четырех частей:
//
//      основная версия
//      дополнительная версия
//      номер сборки
//      номер редакции

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
