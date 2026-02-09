using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
            // Запуск задержки и перехода
            StartSplash();
        }

        private async void StartSplash()
        {
            await Task.Delay(5000); // задержка 5 секунд
            Application.Current.MainPage = new NavigationPage(new MainPage()); 
        }
    }
}
