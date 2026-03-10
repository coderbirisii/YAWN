using System;
using System.Windows;
using System.Windows.Controls;

namespace YAWN.Controls;

public partial class WeaponInventoryControl : UserControl
{
    public static readonly DependencyProperty SkinImageProperty = DependencyProperty.Register(
        "SkinImage",
        typeof(Uri),
        typeof(WeaponInventoryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty SkinNameProperty = DependencyProperty.Register(
        "SkinName",
        typeof(string),
        typeof(WeaponInventoryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty BuddyImageProperty = DependencyProperty.Register(
        "BuddyImage",
        typeof(Uri),
        typeof(WeaponInventoryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty BuddyNameProperty = DependencyProperty.Register(
        "BuddyName",
        typeof(string),
        typeof(WeaponInventoryControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty BuddyVisibilityProperty = DependencyProperty.Register(
        "BuddyVisibility",
        typeof(Visibility),
        typeof(WeaponInventoryControl),
        new PropertyMetadata(Visibility.Collapsed)
    );

    public WeaponInventoryControl()
    {
        InitializeComponent();
    }

    public Uri SkinImage
    {
        get => (Uri)GetValue(SkinImageProperty);
        set => SetValue(SkinImageProperty, value);
    }

    public string SkinName
    {
        get => (string)GetValue(SkinNameProperty);
        set => SetValue(SkinNameProperty, value);
    }

    public Uri BuddyImage
    {
        get => (Uri)GetValue(BuddyImageProperty);
        set => SetValue(BuddyImageProperty, value);
    }

    public string BuddyName
    {
        get => (string)GetValue(BuddyNameProperty);
        set => SetValue(BuddyNameProperty, value);
    }

    public Visibility BuddyVisibility
    {
        get => (Visibility)GetValue(BuddyVisibilityProperty);
        set => SetValue(BuddyVisibilityProperty, value);
    }
}
