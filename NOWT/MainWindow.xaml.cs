using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using NOWT.ViewModels;

namespace NOWT;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MainViewModel>();
        ((App)Application.Current).WindowPlace.Register(this);
    }
}
