using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1
{
    public partial class ImagePage : ContentPage
    {
        public ImagePage(string imageName)
        {
            InitializeComponent();
            DisplayImage.Source = imageName; // Переданное имя картинки
        }
    }
}
