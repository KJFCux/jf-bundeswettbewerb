using CommunityToolkit.Mvvm.Input;
using LagerInsights.Models;

namespace LagerInsights.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}