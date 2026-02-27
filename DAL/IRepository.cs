namespace DAL;

public interface IRepository<TData>
{
    List<(string id, string description)> List(string? username = null);
    string Save(TData data);
    TData Load(string id);
    void Delete(string id);
}