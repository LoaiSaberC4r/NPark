namespace NPark.Application.Abstraction
{
    public interface IEnsureSeeding
    {
        Task SeedDatabaseAsync();
    }
}