using StealAllTheCats.Entities;

namespace StealAllTheCats.Interfaces
{
    public interface ICatService
    {
        Task FetchAndStoreCatsAsync(int limit);
        Task<IEnumerable<CatEntity>> GetCatsAsync(int page, int pageSize);
        // άλλα σχετικά με το domain των γάτων
    }
}
