using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace PLCUnifiedSimulator.GUI.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // タイトルバーのドラッグ機能を設定
        this.Loaded += OnLoaded;
    }
    
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var titleBarDragArea = this.FindControl<Border>("TitleBarDragArea");
        if (titleBarDragArea != null)
        {
            titleBarDragArea.PointerPressed += TitleBar_PointerPressed;
            titleBarDragArea.DoubleTapped += TitleBar_DoubleTapped;
        }
        
        // 初期状態でのボタン更新
        UpdateMaximizeButton();
        
        // プロパティ変更の監視
        this.PropertyChanged += (s, e) =>
        {
            if (e.Property == WindowStateProperty)
            {
                UpdateMaximizeButton();
            }
        };
    }
    
    private void UpdateMaximizeButton()
    {
        var maximizeButton = this.FindControl<Button>("MaximizeButton");
        if (maximizeButton != null)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                maximizeButton.Content = "\uE923"; // Restore icon
                maximizeButton.SetValue(ToolTip.TipProperty, "元に戻す");
            }
            else
            {
                maximizeButton.Content = "\uE922"; // Maximize icon
                maximizeButton.SetValue(ToolTip.TipProperty, "最大化");
            }
        }
    }
    
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
    
    private void TitleBar_DoubleTapped(object? sender, TappedEventArgs e)
    {
        // ダブルクリックで最大化/元に戻す
        this.WindowState = this.WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }
}