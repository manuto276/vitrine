using System;
using System.Collections.Generic;
using System.Windows;
using Wpf.Ui;

namespace Vitrine.Engine.Panel;

internal class PageService : IPageService
{
    private readonly Dictionary<Type, Func<FrameworkElement>> _pages = new();

    internal void Register<T>(Func<T> factory) where T : FrameworkElement
    {
        _pages[typeof(T)] = factory;
    }

    public T? GetPage<T>() where T : class
    {
        return GetPage(typeof(T)) as T;
    }

    public FrameworkElement? GetPage(Type pageType)
    {
        return _pages.TryGetValue(pageType, out var factory) ? factory() : null;
    }
}
