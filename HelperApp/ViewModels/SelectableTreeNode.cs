using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HelperApp.ViewModels;

/// <summary>
/// Узел "плоского" дерева складских позиций.
/// Уровень вложенности задаётся полем Level; дочерние узлы хранятся в Children,
/// но для отображения используется плоский список FlatPositionTree во ViewModel.
/// </summary>
public partial class SelectableTreeNode : ObservableObject
{
    // Ссылка на ViewModel для уведомления об изменении плоского списка
    private Action? _onExpandChanged;

    public SelectableTreeNode(string title, string nodeValue, int level = 0, Action? onExpandChanged = null)
    {
        Title = title;
        NodeValue = nodeValue;
        Level = level;
        _onExpandChanged = onExpandChanged;
    }

    [ObservableProperty]
    private string _title;

    /// <summary>
    /// Значение узла (prefix), используется при создании задачи
    /// </summary>
    public string NodeValue { get; }

    /// <summary>
    /// Уровень вложенности (0 — зона, 1 — тип, 2 — стеллаж, 3 — полка, 4 — ячейка)
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Отступ слева в пикселях (20px на каждый уровень)
    /// </summary>
    public int Indent => Level * 20;

    /// <summary>
    /// Листовой узел (нет потомков)
    /// </summary>
    public bool IsLeaf => Children.Count == 0;

    /// <summary>
    /// Имеет ли узел дочерние элементы (для отображения иконки)
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChevronRotation))]
    private bool _isExpanded = false; // По умолчанию свёрнут

    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Угол поворота шеврона: 0 — свёрнут (►), 90 — развёрнут (▼)
    /// </summary>
    public int ChevronRotation => IsExpanded ? 90 : 0;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                // Рекурсивно выделяем/снимаем выделение у всех дочерних
                foreach (var child in Children)
                {
                    child.IsSelected = value;
                }
            }
        }
    }

    public ObservableCollection<SelectableTreeNode> Children { get; } = new();

    /// <summary>
    /// Команда переключения сворачивания/разворачивания узла.
    /// Уведомляет родительский ViewModel для обновления плоского списка.
    /// </summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        if (!HasChildren) return;
        IsExpanded = !IsExpanded;
        _onExpandChanged?.Invoke();
    }

    /// <summary>
    /// Рекурсивно собирает все листовые узлы, у которых IsSelected == true
    /// </summary>
    public void CollectSelectedLeaves(List<string> result)
    {
        if (IsLeaf && IsSelected)
        {
            result.Add(NodeValue);
        }
        foreach (var child in Children)
        {
            child.CollectSelectedLeaves(result);
        }
    }
}
