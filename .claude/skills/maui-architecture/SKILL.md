---
name: maui-architecture
description: "Extension of dotnet-architecture for .NET MAUI apps. Covers MAUI-specific layers: SQLite model attributes, AutoMapper profiles, AppDatabase registration, ViewModels (CommunityToolkit.Mvvm), XAML Pages, Shell navigation, and MauiProgram.cs DI. Use AFTER or TOGETHER WITH dotnet-architecture for the backend layers (DTO, Domain, Infra.Interfaces, Infra, Application)."
user-invocable: true
---

# .NET MAUI Presentation Layer — Extension of dotnet-architecture

You are an expert assistant that helps developers implement the **MAUI presentation layer** for entities whose backend layers (DTO, Domain, Infra.Interfaces, Infra, Application) follow the Clean Architecture defined in the `dotnet-architecture` skill.

**This skill is an EXTENSION** — it covers ONLY MAUI-specific concerns. For backend layers, invoke or follow `dotnet-architecture`.

## Input

The user will describe the entity to create or modify: `$ARGUMENTS`

## Before You Start

1. **Run `dotnet-architecture` first** (or confirm backend layers already exist) — this skill assumes DTO, Domain Model, Repository Interface, Domain Service, Infra Repository, and Application DI are already in place.
2. Run `dotnet sln list` and `ls` to discover project names and folder layout.
3. The placeholder `{App}` stands for the actual root namespace. Replace it everywhere.
4. **Read at least one existing entity end-to-end** (Model → Mapper → ViewModel → Page) to match style.
5. Check existing UI strings to detect the app language and match it.

---

## What This Skill Covers

| Layer | Responsibility |
|-------|---------------|
| **Infra — AutoMapper (Mappings)** | Profile mapping Domain Model ↔ DTO in `Infra/Mappings/` |
| **Infra — AppDatabase** | SQLite table registration |
| **MAUI — ViewModels** | MVVM with CommunityToolkit.Mvvm, binds to DTOs |
| **MAUI — Pages** | XAML + code-behind with DI |
| **MAUI — Shell** | Navigation registration |
| **MAUI — MauiProgram.cs** | DI for Repos, AppServices, ViewModels, Pages |

## What This Skill Does NOT Cover (handled by dotnet-architecture)

- DTO creation (`{Entity}Info`)
- Domain Model (rich entity with Validate/Update/Create)
- Infra.Interfaces (Repository/AppService interfaces)
- Domain Services
- Infra Repository implementation
- Application DI (`DependencyInjection.cs`)

---

## Architecture Context

### Mapping Flow

```
DB (SQLite) → Repository → Model (Domain) → AutoMapper → {Entity}Info (DTO) → ViewModel → Page
Page → ViewModel → {Entity}Info (DTO) → AutoMapper → Model (Domain) → Repository → DB
```

### MAUI-Specific Conventions

- **ViewModels bind to DTOs** (`{Entity}Info`), never to Domain Models directly
- **Singleton** for repos/services/appservices — **Transient** for ViewModels/Pages
- **AutoMapper** registered once, scans Infra assembly for all Profiles
- Domain Models use **SQLite attributes** (`[PrimaryKey]`, `[AutoIncrement]`, `[Table]`, `[Indexed]`) — this is the only infrastructure concern allowed on Domain Models in MAUI apps
- `CommunityToolkit.Mvvm` source generators: `[ObservableObject]`, `[ObservableProperty]`, `[RelayCommand]`

**Packages:** `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`, `CommunityToolkit.Mvvm`, `AutoMapper.Extensions.Microsoft.DependencyInjection`

---

## Step-by-Step Implementation (MAUI Layers Only)

### Step 1: AutoMapper Profile — `{App}.Infra/Mappings/{Entity}Profile.cs`

> **Recommended:** Use [AutoMapper](https://automapper.org/) (`AutoMapper.Extensions.Microsoft.DependencyInjection`) to map between Domain Models and DTOs. It eliminates repetitive manual mapping code, reduces bugs from forgotten properties, and keeps the mapping logic centralized in Profile classes. Register once with `AddAutoMapper(assembly)` and all Profiles are auto-discovered.

```csharp
using AutoMapper;
using {App}.DTOs;
using {App}.Models;

namespace {App}.Mappings;

public class {Entity}Profile : Profile
{
    public {Entity}Profile()
    {
        CreateMap<{Entity}, {Entity}Info>();          // Model → DTO (read)
        CreateMap<{Entity}Info, {Entity}>()           // DTO → Model (write)
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
```

**Conventions:**
- One Profile per entity in `{App}.Infra/Mappings/`
- Model → Info: direct map (all readable props)
- Info → Model: **ignore** managed fields (`CreatedAt`, `UpdatedAt`) — the entity's `Update()`/`Create()` methods control those
- AutoMapper auto-discovered via `AddAutoMapper(assembly)` in DI

### Step 2: Register Table — `{App}.Infra/Context/AppDatabase.cs`

Add to `InitializeAsync()`: `await _database.CreateTableAsync<{Entity}>();`

### Step 3: List ViewModel — `{App}/ViewModels/{Entity}ListViewModel.cs`

```csharp
public partial class {Entity}ListViewModel : ObservableObject
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;
    public {Entity}ListViewModel(I{Entity}Repository repo, IMapper mapper)
    { _{entity}Repository = repo; _mapper = mapper; }

    [ObservableProperty] private ObservableCollection<{Entity}Info> _items = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        IsLoading = true;
        try
        {
            var models = await _{entity}Repository.GetAllAsync();
            Items = new(_mapper.Map<List<{Entity}Info>>(models));
            IsEmpty = Items.Count == 0;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync({Entity}Info item)
    {
        if (!await Shell.Current.DisplayAlert("Delete", $"Delete \"{item.Name}\"?", "Yes", "No")) return;
        await _{entity}Repository.DeleteAsync(item.Id); Items.Remove(item); IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task GoToDetailAsync({Entity}Info item) =>
        await Shell.Current.GoToAsync("{Entity}DetailPage", new Dictionary<string, object> { { "{Entity}Info", item } });
}
```

### Step 4: Detail ViewModel — `{App}/ViewModels/{Entity}DetailViewModel.cs`

```csharp
public partial class {Entity}DetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;
    public {Entity}DetailViewModel(I{Entity}Repository repo, IMapper mapper)
    { _{entity}Repository = repo; _mapper = mapper; }

    [ObservableProperty] private int _{entity}Id;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isNewItem = true;
    [ObservableProperty] private string _pageTitle = "New {Entity}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("{Entity}Info", out var obj) && obj is {Entity}Info info)
        { {Entity}Id = info.Id; Name = info.Name; IsNewItem = false; PageTitle = "Edit {Entity}"; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var entity = IsNewItem ? {Entity}.Create(Name) : (await _{entity}Repository.GetByIdAsync({Entity}Id))!;
            if (!IsNewItem) entity.Update(Name);
            var error = entity.Validate();
            if (error != null) { await Shell.Current.DisplayAlert("Error", error, "OK"); return; }
            await _{entity}Repository.SaveAsync(entity);
            await Shell.Current.GoToAsync("..");
        }
        catch (InvalidOperationException ex) { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); }
        finally { IsSaving = false; }
    }

    [RelayCommand] private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
```

**ViewModel conventions:**
- ViewModels bind to **`{Entity}Info`** (DTO), not Models
- `IMapper` injected for conversions
- Navigation passes DTOs
- Save/Update goes through domain entity methods (Create/Update/Validate) for business rules
- `[ObservableProperty]` on `_camelCase` fields
- `[RelayCommand]` on `{Method}Async` methods

### Step 5: List Page XAML — `{App}/Pages/{Entity}ListPage.xaml`

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:{App}.ViewModels"
             xmlns:dto="clr-namespace:{App}.DTOs;assembly={App}.DTO"
             x:Class="{App}.Pages.{Entity}ListPage"
             x:DataType="vm:{Entity}ListViewModel"
             Title="{Entities}">
    <Grid RowDefinitions="*,Auto" Padding="16">
        <ActivityIndicator Grid.Row="0" IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}"
                           HorizontalOptions="Center" VerticalOptions="Center" />
        <Label Grid.Row="0" Text="No items yet" IsVisible="{Binding IsEmpty}"
               FontSize="18" HorizontalOptions="Center" VerticalOptions="Center" />
        <CollectionView Grid.Row="0" ItemsSource="{Binding Items}" SelectionMode="None">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="dto:{Entity}Info">
                    <SwipeView>
                        <SwipeView.RightItems><SwipeItems>
                            <SwipeItem Text="Delete" BackgroundColor="#E53935"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:{Entity}ListViewModel}}, Path=DeleteCommand}"
                                CommandParameter="{Binding}" />
                        </SwipeItems></SwipeView.RightItems>
                        <Frame Margin="0,4" Padding="16" CornerRadius="12" BorderColor="Transparent">
                            <Frame.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:{Entity}ListViewModel}}, Path=GoToDetailCommand}"
                                    CommandParameter="{Binding}" />
                            </Frame.GestureRecognizers>
                            <Label Text="{Binding Name}" FontSize="16" FontAttributes="Bold" />
                        </Frame>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
        <Button Grid.Row="1" Text="+ New" Command="{Binding GoToDetailCommand}"
                FontSize="16" HeightRequest="56" CornerRadius="28" Margin="0,12,0,0" />
    </Grid>
</ContentPage>
```

**XAML conventions:**
- DTOs need `assembly={App}.DTO` in xmlns
- ViewModels in MAUI do NOT need assembly qualifier
- Use `x:DataType` for compiled bindings

### Step 6: Page Code-Behind — `{App}/Pages/{Entity}ListPage.xaml.cs`

```csharp
public partial class {Entity}ListPage : ContentPage
{
    private readonly {Entity}ListViewModel _viewModel;
    public {Entity}ListPage({Entity}ListViewModel viewModel)
    { InitializeComponent(); BindingContext = _viewModel = viewModel; }

    protected override async void OnAppearing()
    { base.OnAppearing(); await _viewModel.LoadItemsCommand.ExecuteAsync(null); }
}
```

### Step 7: DI Registration — `{App}/MauiProgram.cs`

```csharp
// AutoMapper — scans Infra assembly for all Profiles (register ONCE)
builder.Services.AddAutoMapper(typeof(AppDatabase).Assembly);

// Application Services (Domain Services — registered via Application project)
builder.Services.AddApplicationServices();

// Infrastructure
builder.Services.AddSingleton<I{Entity}AppService, {Entity}AppService>();      // AppService (if needed)
builder.Services.AddSingleton<I{Entity}Repository, {Entity}Repository>();      // Repository

// MAUI Presentation
builder.Services.AddTransient<{Entity}ListViewModel>();                        // ViewModels
builder.Services.AddTransient<{Entity}DetailViewModel>();
builder.Services.AddTransient<{Entity}ListPage>();                             // Pages
builder.Services.AddTransient<{Entity}DetailPage>();
```

**Key points:**
- `AddApplicationServices()` comes from `{App}.Application` project — registers all Domain Services
- Domain Services are NOT registered individually in `MauiProgram.cs`
- **Singleton** for repos/services/appservices — **Transient** for ViewModels/Pages
- AutoMapper registered **once**, scans assembly for all Profiles

### Step 8: Shell Navigation — `AppShell`

In `.xaml`: `<ShellContent Title="{Entities}" ContentTemplate="{DataTemplate pages:{Entity}ListPage}" Route="{Entity}ListPage" />`

In `.xaml.cs`: `Routing.RegisterRoute("{Entity}DetailPage", typeof({Entity}DetailPage));`

### Step 9: Unit Tests — `{App}.Tests/Services/{Entity}RepositoryTests.cs`

```csharp
public class {Entity}RepositoryTests : IAsyncLifetime
{
    private AppDatabase _database = null!;
    private {Entity}Repository _repository = null!;
    private string _dbPath = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db3");
        _database = new AppDatabase(_dbPath); await _database.InitializeAsync();
        _repository = new {Entity}Repository(_database);
    }
    public Task DisposeAsync() { if (File.Exists(_dbPath)) File.Delete(_dbPath); return Task.CompletedTask; }

    [Fact] public async Task SaveAsync_Insert() { Assert.Equal(1, await _repository.SaveAsync({Entity}.Create("Test"))); }
    [Fact] public async Task GetAllAsync() { /* save 2, assert count == 2 */ }
    [Fact] public async Task SaveAsync_Update() { /* save, Update(), save again, assert new value */ }
    [Fact] public async Task DeleteAsync() { /* save, delete, get returns null */ }

    // Mapper tests
    [Fact] public void MapModelToInfo()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<{Entity}Profile>());
        var mapper = config.CreateMapper();
        var model = {Entity}.Create("Test");
        var info = mapper.Map<{Entity}Info>(model);
        Assert.Equal(model.Name, info.Name);
    }
}
```

---

## Checklist

| # | Layer | File | Notes |
|---|-------|------|-------|
| 0 | **Backend** | Run `dotnet-architecture` skill | DTO, Domain, Infra.Interfaces, Infra Repo, Application DI |
| 1 | Infra | `{App}.Infra/Mappings/{Entity}Profile.cs` | AutoMapper Profile: Model ↔ Info |
| 2 | Infra | Modify `AppDatabase.cs` → `CreateTableAsync<{Entity}>()` | SQLite table |
| 3-4 | MAUI | `ViewModels/{Entity}ListViewModel.cs` + `{Entity}DetailViewModel.cs` | MVVM |
| 5-6 | MAUI | `Pages/{Entity}ListPage.xaml(.cs)` + `{Entity}DetailPage.xaml(.cs)` | UI |
| 7 | MAUI | Modify `MauiProgram.cs` | DI: Repos, AppServices, VMs, Pages + `AddApplicationServices()` |
| 8 | MAUI | Modify `AppShell.xaml` + `.xaml.cs` | Navigation |
| 9 | Tests | `{Entity}RepositoryTests.cs` + mapper tests | Verification |

---

## Response Guidelines

1. **Confirm backend exists** — Check that DTO, Domain Model, Repository Interface/Implementation, and Application DI are in place. If not, run `dotnet-architecture` first.
2. **Discover first** — `dotnet sln list`, read existing entities to match patterns
3. **Order** — Mapper → AppDatabase → ViewModels → Pages → MauiProgram.cs → Shell → Tests
4. **Build after each layer** to catch errors early
5. **Match the app language** in UI strings
6. **ViewModels bind to DTOs** — never to Domain Models directly
7. **Mapper** — one Profile per entity in `Infra/Mappings/`
8. **Singleton** for repos/services/appservices — **Transient** for ViewModels/Pages
9. **Domain logic stays in Domain** — no business rules in ViewModels or Pages
10. **AutoMapper** registered once in `MauiProgram.cs`, scans Infra assembly