using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PLCUnifiedSimulator.GUI.Avalonia.ViewModels;
using System;

namespace PLCUnifiedSimulator.GUI.Avalonia;

public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "Data is null" };

        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}