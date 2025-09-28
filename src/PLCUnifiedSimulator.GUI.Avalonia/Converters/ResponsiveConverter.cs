using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PLCUnifiedSimulator.GUI.Avalonia.Converters;

/// <summary>
/// レスポンシブデザインのためのコンバーター
/// 画面幅に応じてレイアウトを調整する
/// </summary>
public class ResponsiveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && parameter is string breakpoint)
        {
            return double.TryParse(breakpoint, out var bp) && width >= bp;
        }
        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 画面サイズに応じたColumn定義を返すコンバーター
/// </summary>
public class ResponsiveColumnConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // タブレット以上のサイズ（768px以上）では2カラム、それ以下では1カラム
            return width >= 768 ? "1*,1*" : "*";
        }
        return "*";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 画面サイズに応じたボタンの最小幅を返すコンバーター
/// </summary>
public class ResponsiveButtonWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // 大画面では広めのボタン、小画面では狭めのボタン
            return width >= 600 ? 120.0 : 100.0;
        }
        return 100.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 画面サイズに応じたダッシュボードカードのグリッド列定義を返すコンバーター
/// </summary>
public class ResponsiveDashboardConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // ≥1200px: 3カラム, ≥768px: 2カラム, <768px: 1カラム
            if (width >= 1200) return "*,*,*";
            if (width >= 768) return "*,*";
            return "*";
        }
        return "*,*,*";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 画面サイズに応じたナビゲーションレールの幅を返すコンバーター
/// </summary>
public class ResponsiveNavRailConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // 小画面では非表示、中画面以上では表示
            return width >= 768 ? 80.0 : 0.0;
        }
        return 80.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 画面サイズに応じたナビゲーションレールの可視性を返すコンバーター
/// </summary>
public class ResponsiveNavVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return width >= 768;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 画面サイズに応じたマージンを返すコンバーター
/// </summary>
public class ResponsiveMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // 大画面: 32px, 中画面: 24px, 小画面: 16px
            if (width >= 1200) return "32";
            if (width >= 768) return "24";
            return "16";
        }
        return "24";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 画面サイズに応じたアイテム配置カラム数を返すコンバーター
/// </summary>
public class ResponsiveUniformGridConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // ダッシュボードカード用: ≥1000px: 3カラム, ≥600px: 2カラム, <600px: 1カラム
            if (width >= 1000) return 3;
            if (width >= 600) return 2;
            return 1;
        }
        return 2;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}