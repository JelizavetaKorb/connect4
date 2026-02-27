using BLL;
using DAL;

namespace WebApp;

public class RepositoryProvider
{
    public IRepository<GameState> ActiveRepo { get; }
    public IRepository<GameState> OtherRepo { get; }

    public RepositoryProvider(IRepository<GameState> activeRepo, IRepository<GameState> otherRepo)
    {
        ActiveRepo = activeRepo;
        OtherRepo = otherRepo;
    }
}