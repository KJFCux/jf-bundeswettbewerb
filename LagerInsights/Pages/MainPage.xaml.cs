using LagerInsights.Models;
using LagerInsights.PageModels;

namespace LagerInsights.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}