namespace UI.SharedComponents.Base;

public interface IViewModelStore
{
    TViewModel? Get<TViewModel>(Guid id) where TViewModel : class, new();
    void Set<TViewModel>(Guid id, TViewModel? viewModel) where TViewModel : class;
}