using Cogs.Disposal;
using Cogs.Reflection;
using System.Reflection;

namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Sets the items source of an items control to a collection that loads elements as they are needed for display
/// </summary>
/// <typeparam name="TControl">The type of items control</typeparam>
public abstract class ItemsControlDataVirtualization<TControl> :
    Behavior<TControl>
    where TControl : ItemsControl
{
    FastMethodInfo? loadCapacitySelectorSetter;
    FastMethodInfo? refreshLoadCapacity;
    FastMethodInfo? setLoadCapacity;

    /// <summary>
    /// Gets the data virtualization list
    /// </summary>
    public IDisposable? List
    {
        get => (IDisposable?)GetValue(listPropertyKey.DependencyProperty);
        private set => SetValue(listPropertyKey, value);
    }

    /// <summary>
    /// Gets/sets the source upon which the data virtualization list is based
    /// </summary>
    public object? Source
    {
        get => (object?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    void AssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        if (List is { } list)
            refreshLoadCapacity?.Invoke(list);
        if (List is null || List is IDisposalStatus disposalStatus && disposalStatus.IsDisposed)
        {
            InitializeList();
            if (GetScrollViewer() is { } scrollViewer)
                scrollViewer.SizeChanged += ScrollViewerSizeChanged;
        }
    }

    void AssociatedObjectUnloaded(object sender, RoutedEventArgs e)
    {
        if (List is { } list)
            list.Dispose();
    }

    /// <summary>
    /// Gets the scroll viewer control the viewport size of which will be used to manage the data virtualization list's load capacity
    /// </summary>
    /// <returns>The scroll viewer control or <c>null</c></returns>
    protected abstract ScrollViewer? GetScrollViewer();

    void InitializeList()
    {
        if (AssociatedObject is { } associatedObject && CreateList(Source) is { } list)
        {
            List = list;
            var listType = list.GetType();
            loadCapacitySelectorSetter =
                listType.GetProperty("LoadCapacitySelector", BindingFlags.NonPublic | BindingFlags.Instance) is { } loadCapacitySelectorProperty &&
                loadCapacitySelectorProperty.SetMethod is { } loadCapacitySelectorPropertySetMethodInfo
                ?
                FastMethodInfo.Get(loadCapacitySelectorPropertySetMethodInfo)
                :
                throw new Exception("Could not find list LoadCapacitySelector property setter");
            refreshLoadCapacity =
                listType.GetMethod("RefreshLoadCapacity", BindingFlags.NonPublic | BindingFlags.Instance) is { } refreshLoadCapacityMethodInfo
                ?
                FastMethodInfo.Get(refreshLoadCapacityMethodInfo)
                :
                throw new Exception("Could not find list RefreshLoadCapacity method");
            setLoadCapacity =
                listType.GetMethod("SetLoadCapacity", BindingFlags.NonPublic | BindingFlags.Instance) is { } setLoadCapacityMethodInfo
                ?
                FastMethodInfo.Get(setLoadCapacityMethodInfo)
                :
                throw new Exception("Could not find list SetLoadCapacity method");
            LinkScrollViewer();
        }
        else if (List is { } listToDispose)
        {
            loadCapacitySelectorSetter = null;
            refreshLoadCapacity = null;
            setLoadCapacity = null;
            listToDispose.Dispose();
        }
    }

    /// <summary>
    /// Causes the associated object's scroll viewer's viewport to determine the load capacity of the data virtualization list
    /// </summary>
    protected void LinkScrollViewer()
    {
        if (AssociatedObject is { } associatedObject && List is { } list && loadCapacitySelectorSetter is not null && GetScrollViewer() is { } scrollViewer && refreshLoadCapacity is not null)
        {
            loadCapacitySelectorSetter.Invoke(list, new[] { () => (int)scrollViewer.ViewportHeight * 3 + 1 });
            if (scrollViewer.ViewportHeight == 0)
                associatedObject.Dispatcher.InvokeAsync(() => refreshLoadCapacity?.Invoke(list), DispatcherPriority.ContextIdle);
            else
                refreshLoadCapacity.Invoke(list);
        }
    }

    /// <summary>
    /// Called after the behavior is attached to an associated object
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        InitializeList();
        BindingOperations.SetBinding(AssociatedObject, ItemsControl.ItemsSourceProperty, new Binding(nameof(List))
        {
            Mode = BindingMode.OneWay,
            Source = this
        });
        AssociatedObject.Loaded += AssociatedObjectLoaded;
        AssociatedObject.Unloaded += AssociatedObjectUnloaded;
        if (GetScrollViewer() is { } scrollViewer)
            scrollViewer.SizeChanged += ScrollViewerSizeChanged;
    }

    /// <summary>
    /// Called when the behavior is being detached from its associated object, but before it has actually occurred
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= AssociatedObjectLoaded;
        AssociatedObject.Unloaded -= AssociatedObjectUnloaded;
        if (GetScrollViewer() is { } scrollViewer)
            scrollViewer.SizeChanged -= ScrollViewerSizeChanged;
        BindingOperations.ClearBinding(AssociatedObject, ItemsControl.ItemsSourceProperty);
        InitializeList();
    }

    void ScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (List is { } list)
        {
            setLoadCapacity?.Invoke(list, int.MaxValue);
            AssociatedObject?.Dispatcher.InvokeAsync(() => refreshLoadCapacity?.Invoke(list), DispatcherPriority.ContextIdle);
        }
    }

    static readonly DependencyPropertyKey listPropertyKey = DependencyProperty.RegisterReadOnly(nameof(List), typeof(IDisposable), typeof(ItemsControlDataVirtualization<TControl>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="Source"/> dependency property
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(ItemsControlDataVirtualization<TControl>), new PropertyMetadata(null, OnSourceChanged));

    static IDisposable? CreateList(object? source) =>
        source?.GetType() is { } sourceType &&
        sourceType.GetInterfaces() is { } sourceInterfaces &&
        sourceInterfaces.FirstOrDefault
        (
            sourceInterface
            =>
            sourceInterface.IsGenericType &&
            sourceInterface.GetGenericTypeDefinition() is { } sourceInterfaceGenericTypeDefinition &&
            sourceInterfaceGenericTypeDefinition == typeof(IReadOnlyList<>)
        ) is { } sourceReadOnlyListInterface &&
        typeof(DataVirtualizationList<>) is { } listGenericTypeDefinition &&
        listGenericTypeDefinition.MakeGenericType(sourceReadOnlyListInterface.GenericTypeArguments) is { } listType &&
        listType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { sourceReadOnlyListInterface }, null) is { } listConstructor
        ?
        (IDisposable?)listConstructor.Invoke(new[] { source })
        :
        null;

    static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ItemsControlDataVirtualization<TControl> virtualization)
        {
            var oldList = virtualization.List;
            virtualization.InitializeList();
            oldList?.Dispose();
        }
    }
}
