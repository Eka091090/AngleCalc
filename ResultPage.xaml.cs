using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1
{
    public partial class ResultPage : ContentPage
    {
        public ResultPage(string resultText)
        {
            InitializeComponent();
            ResultTextLabel.Text = resultText;
        }
    }
}
