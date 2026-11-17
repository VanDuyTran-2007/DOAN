using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace DOAN
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Hiển thị ngày dd/MM/yyyy và tiền 1.000.000 theo kiểu Việt Nam
            var vi = new CultureInfo("vi-VN");
            CultureInfo.DefaultThreadCurrentCulture = vi;
            CultureInfo.DefaultThreadCurrentUICulture = vi;
            Thread.CurrentThread.CurrentCulture = vi;
            Thread.CurrentThread.CurrentUICulture = vi;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(vi.IetfLanguageTag)));

            base.OnStartup(e);
        }
    }
}
