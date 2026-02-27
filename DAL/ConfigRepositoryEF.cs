using BLL;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class ConfigRepositoryEF : IRepository<GameState>
{
    private readonly AppDbContext _dbContext;

    public ConfigRepositoryEF(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // if username given, filters by it
    public List<(string id, string description)> List(string? username = null)
    {
        var query = _dbContext.GameStates
            .Include(g => g.Configuration)
            .AsEnumerable();

        if (!string.IsNullOrEmpty(username))
        {
            query = query.Where(g => 
                g.Configuration.Player1Name.Equals(username, StringComparison.OrdinalIgnoreCase) || 
                g.Configuration.Player2Name.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .Select(g => (g.Id.ToString(), g.Name))
            .ToList();
    }

    // first checks if state exists already. if yes, updates, else adds.
    public string Save(GameState state)
    {
        var existing = _dbContext.GameStates
            .Include(g => g.Configuration)
            .FirstOrDefault(g => g.Id == state.Id);

        if (existing == null)
        {
            _dbContext.GameStates.Add(state);
        }
        else
        {
            existing.Name = state.Name;
            existing.Board = state.Board;
            existing.IsNextPlayerX = state.IsNextPlayerX;
        }

        _dbContext.SaveChanges();
        return state.Id.ToString();
    }

    public GameState Load(string id)
    {
        var guid = Guid.Parse(id);
        var state = _dbContext.GameStates
            .Include(g => g.Configuration)
            .AsNoTracking()
            .FirstOrDefault(g => g.Id == guid);

        if (state == null) throw new NullReferenceException("Game state not found: " + id);
        return state;
    }
    
    public void Delete(string id)
    {
        if (!Guid.TryParse(id, out var guidId))
            return;
        var entity = _dbContext.GameStates
            .Include(g => g.Configuration)
            .FirstOrDefault(g => g.Id == guidId);
        
        if (entity == null)
            return;

        var configId = entity.ConfigurationId;
    
        _dbContext.GameStates.Remove(entity);
        _dbContext.SaveChanges();
    
        // deletes orphaned configs
        var configStillUsed = _dbContext.GameStates
            .Any(gs => gs.ConfigurationId == configId);
    
        if (!configStillUsed)
        {
            var config = _dbContext.GameConfigurations
                .FirstOrDefault(c => c.Id == configId);
        
            if (config != null)
            {
                _dbContext.GameConfigurations.Remove(config);
                _dbContext.SaveChanges();
            }
        }
    }
}