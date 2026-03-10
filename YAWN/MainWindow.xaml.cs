using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using YAWN.ViewModels;

namespace YAWN;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MainViewModel>();
        ((App)Application.Current).WindowPlace.Register(this);
    }
}
