using Cogs.Disposal;

namespace Cogs.Wpf.Behaviors;

/// <summary>
/// Sets the items source of an items control to a collection that loads elements as they are needed for display
/// </summary>
/// <typeparam name="TControl">The type of items control</typeparam>
public abstract class ItemsControlDataVirtualization<TControl> :
    Behavior<TControl>
    where TControl : ItemsControl
{
    /// <summary>
    /// Gets/sets the number of additional items to load before and after the visible items
    /// </summary>
    public int AdditionalItems
    {
        get => (int)GetValue(AdditionalItemsProperty);
        set => SetValue(AdditionalItemsProperty, value);
    }

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
        if (List is null || List is IDisposalStatus disposalStatus && disposalStatus.IsDisposed)
        {
            List = null;
            InitializeList();
        }
    }

    void AssociatedObjectUnloaded(object sender, RoutedEventArgs e) =>
        TerminateList();

    /// <summary>
    /// Gets the scroll viewer control the viewport size of which will be used to manage the data virtualization list's load capacity
    /// </summary>
    /// <returns>The scroll viewer control or <c>null</c></returns>
    protected abstract ScrollViewer? GetScrollViewer();

    /// <summary>
    /// Attempts to initialize the load-managed list
    /// </summary>
    protected void InitializeList()
    {
        if (List is null && AssociatedObject is { } associatedObject && Source is IList sourceList)
        {
            if (GetScrollViewer() is { } scrollViewer)
                List = new ScrollViewerDataVirtualizationList(scrollViewer, sourceList, AdditionalItems);
            else
                _ = Task.Run(async () =>
                {
                    await associatedObject.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background).Task.ConfigureAwait(false);
                    await associatedObject.Dispatcher.InvokeAsync(() =>
                    {
                        if (GetScrollViewer() is { } scrollViewer)
                            List = new ScrollViewerDataVirtualizationList(scrollViewer, sourceList, AdditionalItems);
                    }, DispatcherPriority.Normal).Task.ConfigureAwait(false);
                });
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
    }

    /// <summary>
    /// Called when the behavior is being detached from its associated object, but before it has actually occurred
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= AssociatedObjectLoaded;
        AssociatedObject.Unloaded -= AssociatedObjectUnloaded;
        BindingOperations.ClearBinding(AssociatedObject, ItemsControl.ItemsSourceProperty);
        TerminateList();
    }

    void TerminateList()
    {
        if (List is not null)
        {
            List.Dispose();
            List = null;
        }
    }

    static readonly DependencyPropertyKey listPropertyKey = DependencyProperty.RegisterReadOnly(nameof(List), typeof(IDisposable), typeof(ItemsControlDataVirtualization<TControl>), new PropertyMetadata(defaultValue: null));

    /// <summary>
    /// Identifies the <see cref="AdditionalItems"/> dependency property
    /// </summary>
    public static readonly DependencyProperty AdditionalItemsProperty = DependencyProperty.Register(nameof(AdditionalItems), typeof(int), typeof(ItemsControlDataVirtualization<TControl>), new PropertyMetadata(0, OnAdditionalItemsChanged));

    /// <summary>
    /// Identifies the <see cref="Source"/> dependency property
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(object), typeof(ItemsControlDataVirtualization<TControl>), new PropertyMetadata(null, OnSourceChanged));

    static void OnAdditionalItemsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ItemsControlDataVirtualization<TControl> virtualization &&
            e.NewValue is int additionalItems &&
            virtualization.List is ScrollViewerDataVirtualizationList list)
            list.AdditionalItems = additionalItems;
    }

    static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ItemsControlDataVirtualization<TControl> virtualization)
        {
            var oldList = virtualization.List;
            virtualization.List = null;
            virtualization.InitializeList();
            oldList?.Dispose();
        }
    }
}
