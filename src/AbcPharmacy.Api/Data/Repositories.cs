using AbcPharmacy.Api.Models;

namespace AbcPharmacy.Api.Data;


public interface IMedicineRepository
{
    Task<IReadOnlyList<Medicine>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Medicine medicine, CancellationToken cancellationToken = default);

    
    Task<Medicine?> UpdateAsync(Guid id, Action<Medicine> change, CancellationToken cancellationToken = default);
}

public interface ISaleRepository
{
    Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
}

public sealed class JsonMedicineRepository(JsonFileStore<Medicine> store) : IMedicineRepository
{
    public async Task<IReadOnlyList<Medicine>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await store.ReadAllAsync(cancellationToken);

    public async Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await store.ReadAllAsync(cancellationToken)).FirstOrDefault(m => m.Id == id);

    public Task AddAsync(Medicine medicine, CancellationToken cancellationToken = default) =>
        store.UpdateAsync(items =>
        {
            items.Add(medicine);
            return true;
        }, cancellationToken);

    public Task<Medicine?> UpdateAsync(Guid id, Action<Medicine> change, CancellationToken cancellationToken = default) =>
        store.UpdateAsync(items =>
        {
            var medicine = items.FirstOrDefault(m => m.Id == id);
            if (medicine is not null)
            {
                change(medicine);
            }

            return medicine;
        }, cancellationToken);
}

public sealed class JsonSaleRepository(JsonFileStore<Sale> store) : ISaleRepository
{
    public async Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await store.ReadAllAsync(cancellationToken);

    public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) =>
        store.UpdateAsync(items =>
        {
            items.Add(sale);
            return true;
        }, cancellationToken);
}
