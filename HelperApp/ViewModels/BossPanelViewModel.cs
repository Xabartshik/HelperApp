using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperApp.Models.BossPanel;
using HelperApp.Services;

namespace HelperApp.ViewModels;

public partial class BossPanelViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    public BossPanelViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        ActiveTasks = new ObservableCollection<BossPanelTaskCardDto>();
        EmployeeWorkloads = new ObservableCollection<EmployeeWorkloadDto>();
        AvailableEmployees = new ObservableCollection<AvailableEmployeeDto>();
        PositionTree = new ObservableCollection<SelectableTreeNode>();
        FlatPositionTree = new ObservableCollection<SelectableTreeNode>();
        SelectableEmployees = new ObservableCollection<SelectableItem<AvailableEmployeeDto>>();
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isCreateFormExpanded = false;

    // Списки для UI
    public ObservableCollection<BossPanelTaskCardDto> ActiveTasks { get; }
    public ObservableCollection<EmployeeWorkloadDto> EmployeeWorkloads { get; }
    public ObservableCollection<AvailableEmployeeDto> AvailableEmployees { get; }

    // Дерево позиций: иерархическое (для логики) и плоское (для отображения в MAUI)
    public ObservableCollection<SelectableTreeNode> PositionTree { get; }
    public ObservableCollection<SelectableTreeNode> FlatPositionTree { get; }

    public ObservableCollection<SelectableItem<AvailableEmployeeDto>> SelectableEmployees { get; }

    private List<PositionCellDto> _allPositions = new();

    [ObservableProperty]
    private string _positionSearchText = string.Empty;

    partial void OnPositionSearchTextChanged(string value)
    {
        FilterPositionTree(value);
    }

    // Для создания инвентаризации
    [ObservableProperty]
    private int _newInventoryWorkerCount = 1;

    [ObservableProperty]
    private string _newInventoryDescription = string.Empty;

    [ObservableProperty]
    private int _newInventoryPriority = 5;

    [ObservableProperty]
    private int _autoSelectWorkerCount = 1;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await Task.WhenAll(
                LoadActiveTasksAsync(),
                LoadEmployeeWorkloadAsync(),
                LoadAvailableEmployeesAsync(),
                LoadAvailablePositionsAsync()
            );
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadActiveTasksAsync()
    {
        var tasks = await _apiClient.GetBossPanelActiveTasksAsync();
        ActiveTasks.Clear();
        if (tasks != null)
        {
            foreach (var t in tasks) ActiveTasks.Add(t);
        }
    }

    private async Task LoadEmployeeWorkloadAsync()
    {
        var workloads = await _apiClient.GetBossPanelEmployeeWorkloadAsync();
        EmployeeWorkloads.Clear();
        if (workloads != null)
        {
            foreach (var w in workloads.OrderByDescending(x => x.ActiveTasksCount)) 
                EmployeeWorkloads.Add(w);
        }
    }

    private async Task LoadAvailableEmployeesAsync()
    {
        var available = await _apiClient.GetBossPanelAvailableEmployeesAsync();
        AvailableEmployees.Clear();
        SelectableEmployees.Clear();
        if (available != null)
        {
            // Сначала рекомендованные (с наименьшей нагрузкой)
            foreach (var e in available.OrderByDescending(x => x.IsRecommended).ThenBy(x => x.ActiveTasksCount)) 
            {
                AvailableEmployees.Add(e);
                SelectableEmployees.Add(new SelectableItem<AvailableEmployeeDto>(e));
            }
        }
    }

    private async Task LoadAvailablePositionsAsync()
    {
        var positions = await _apiClient.GetBossPanelPositionsAsync();
        _allPositions.Clear();
        if (positions != null)
        {
            _allPositions.AddRange(positions);
        }
        BuildPositionTree();
    }

    private void BuildPositionTree()
    {
        PositionTree.Clear();
        if (!_allPositions.Any())
        {
            RefreshFlatTree();
            return;
        }

        // Уровень 1: Зоны
        var zones = _allPositions.GroupBy(p => p.ZoneCode).OrderBy(g => g.Key);
        foreach (var zoneGroup in zones)
        {
            var branchId = zoneGroup.First().BranchId;
            var zonePrefix = $"{branchId}-{zoneGroup.Key}";
            var zoneNode = new SelectableTreeNode($"Зона: {zoneGroup.Key}", zonePrefix, 0, RefreshFlatTree);

            // Уровень 2: Стеллаж/ёмкость (FLSNumber + тип в скобках справа)
            var flsGroups = zoneGroup.GroupBy(p => (p.FirstLevelStorageType, p.FLSNumber)).OrderBy(g => g.Key.FLSNumber);
            foreach (var flsGroup in flsGroups)
            {
                var (flsType, flsNumber) = flsGroup.Key;
                // Префикс включает тип, чтобы быть совместимым с серверным поиском по StartsWith
                var flsPrefix = $"{zonePrefix}-{flsType}-{flsNumber}";
                var flsTitle = $"{flsNumber}  ·  {flsType}";
                var flsNode = new SelectableTreeNode(flsTitle, flsPrefix, 1, RefreshFlatTree);

                // Уровень 3: SecondLevelStorage (полка)
                var secondLevels = flsGroup.Where(p => !string.IsNullOrEmpty(p.SecondLevelStorage))
                                           .GroupBy(p => p.SecondLevelStorage!)
                                           .OrderBy(g => g.Key);
                foreach (var sndGroup in secondLevels)
                {
                    var sndPrefix = $"{flsPrefix}-{sndGroup.Key}";
                    var sndNode = new SelectableTreeNode($"Полка {sndGroup.Key}", sndPrefix, 2, RefreshFlatTree);

                    // Уровень 4: ThirdLevelStorage (ячейка)
                    var thirdLevels = sndGroup.Where(p => !string.IsNullOrEmpty(p.ThirdLevelStorage))
                                              .GroupBy(p => p.ThirdLevelStorage!)
                                              .OrderBy(g => g.Key);
                    foreach (var trdGroup in thirdLevels)
                    {
                        var trdPrefix = $"{sndPrefix}-{trdGroup.Key}";
                        var trdNode = new SelectableTreeNode($"Ячейка {trdGroup.Key}", trdPrefix, 3, RefreshFlatTree);
                        sndNode.Children.Add(trdNode);
                    }

                    flsNode.Children.Add(sndNode);
                }

                zoneNode.Children.Add(flsNode);
            }
            PositionTree.Add(zoneNode);
        }

        RefreshFlatTree();
    }

    /// <summary>
    /// Перестраивает плоский список видимых узлов для привязки в CollectionView.
    /// Вызывается при инициализации дерева или нажатии на узел.
    /// </summary>
    private void RefreshFlatTree()
    {
        FlatPositionTree.Clear();
        foreach (var rootNode in PositionTree)
        {
            FlattenNode(rootNode, FlatPositionTree, parentVisible: true);
        }
    }

    private void FlattenNode(SelectableTreeNode node, ObservableCollection<SelectableTreeNode> list, bool parentVisible)
    {
        // Текущий узел виден, если его родитель развёрнут и сам он IsVisible
        if (!parentVisible || !node.IsVisible) return;
        list.Add(node);

        // Дочерние узлы видны только если текущий узел развёрнут
        if (node.HasChildren && node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                FlattenNode(child, list, parentVisible: true);
            }
        }
    }

    /// <summary>
    /// Рекурсивная фильтрация дерева позиций по поисковому тексту.
    /// </summary>
    private void FilterPositionTree(string searchText)
    {
        searchText = searchText?.ToLowerInvariant() ?? string.Empty;
        bool isSearching = !string.IsNullOrEmpty(searchText);

        foreach (var node in PositionTree)
        {
            FilterNodeRecursive(node, searchText, isSearching);
        }

        // Обновляем плоский список после фильтрации
        RefreshFlatTree();
    }

    private bool FilterNodeRecursive(SelectableTreeNode node, string search, bool isSearching)
    {
        bool selfMatch = string.IsNullOrEmpty(search) || node.Title.ToLowerInvariant().Contains(search);
        bool anyChildVisible = false;

        foreach (var child in node.Children)
        {
            bool childVisible = FilterNodeRecursive(child, search, isSearching);
            if (childVisible) anyChildVisible = true;
        }

        bool isVisible = selfMatch || anyChildVisible;
        node.IsVisible = isVisible;

        // При поиске разворачиваем совпадающие узлы
        if (isSearching && isVisible)
            node.IsExpanded = true;
        else if (!isSearching)
            node.IsExpanded = false; // Сворачиваем при сбросе поиска

        return isVisible;

    }

    [RelayCommand]
    private void ToggleCreateForm()
    {
        IsCreateFormExpanded = !IsCreateFormExpanded;
    }

    [RelayCommand]
    private async Task TaskTappedAsync(ActiveTaskBriefDto task)
    {
        if (task == null) return;
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Детали задачи", 
                $"Название: {task.Title}\nТип: {task.TaskType}\nСтатус: {task.Status}", "ОК");
        }
    }

    [RelayCommand]
    private async Task AutoSelectEmployeesAsync()
    {
        if (AutoSelectWorkerCount < 1)
        {
            ErrorMessage = "Количество для подбора должно быть больше 0";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var selectedIds = await _apiClient.GetBossPanelAutoSelectedEmployeesAsync(AutoSelectWorkerCount);
            if (selectedIds != null)
            {
                foreach (var emp in SelectableEmployees)
                {
                    emp.IsSelected = false;
                }

                int matchedCount = 0;
                foreach (var id in selectedIds)
                {
                    var emp = SelectableEmployees.FirstOrDefault(e => e.Item.EmployeeId == id);
                    if (emp != null)
                    {
                        emp.IsSelected = true;
                        matchedCount++;
                    }
                }

                if (matchedCount == 0)
                {
                    ErrorMessage = "Не удалось подобрать сотрудников. Возможно, их нет на смене.";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка автоподбора: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateInventoryAsync()
    {
        if (NewInventoryWorkerCount < 1)
        {
            ErrorMessage = "Количество работников должно быть больше 0";
            return;
        }

        var selectedPrefixes = new List<string>();
        
        void CollectSelectedLeaves(SelectableTreeNode node)
        {
            if (node.IsLeaf && node.IsSelected)
                selectedPrefixes.Add(node.NodeValue);
            
            foreach (var child in node.Children)
                CollectSelectedLeaves(child);
        }

        foreach (var root in PositionTree)
            CollectSelectedLeaves(root);

        var selectedEmployees = SelectableEmployees.Where(e => e.IsSelected).Select(e => e.Item.EmployeeId).ToList();

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var dto = new CreateInventoryByZoneDto
            {
                Priority = NewInventoryPriority,
                WorkerCount = NewInventoryWorkerCount,
                Description = string.IsNullOrWhiteSpace(NewInventoryDescription) ? "Инвентаризация" : NewInventoryDescription,
                ZonePrefixes = selectedPrefixes,
                WorkerIds = selectedEmployees.Count > 0 ? selectedEmployees : null
            };

            var result = await _apiClient.CreateBossPanelInventoryTaskByZoneAsync(dto);
            if (result != null)
            {
                await LoadDataAsync(); // Обновляем данные
                
                // Очистка формы
                NewInventoryDescription = string.Empty;
                NewInventoryWorkerCount = 1;
                NewInventoryPriority = 5;
                AutoSelectWorkerCount = 1;
                PositionSearchText = string.Empty;
                BuildPositionTree(); // Reset tree state
                foreach (var e in SelectableEmployees) e.IsSelected = false;
                IsCreateFormExpanded = false;

                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Успех", result.Message ?? "Задача успешно создана", "ОК");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка создания задачи: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class SelectableItem<T> : ObservableObject
{
    [ObservableProperty]
    private T _item;

    [ObservableProperty]
    private bool _isSelected;

    public SelectableItem(T item, bool isSelected = false)
    {
        Item = item;
        IsSelected = isSelected;
    }
}
