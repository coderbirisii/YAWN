using System.Windows;

namespace YAWN;

public interface IViewFactory
{
    FrameworkElement? ResolveView(object viewModel);
}
