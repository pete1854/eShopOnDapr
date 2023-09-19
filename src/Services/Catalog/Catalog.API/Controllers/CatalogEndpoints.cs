using Microsoft.AspNetCore.Http.HttpResults;

namespace Microsoft.eShopOnDapr.Services.Catalog.API.Controllers;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/catalog");

        group.MapGet("brands", CatalogBrandsAsync);

        group.MapGet("types", CatalogTypesAsync);

        group.MapGet("items/by_ids", ItemsByIdAsync);

        group.MapGet("items/by_page", ItemsAsync);
    }

    public static Task<List<CatalogBrand>> CatalogBrandsAsync(CatalogDbContext _context) =>
        _context.CatalogBrands.ToListAsync();

    public static Task<List<CatalogType>> CatalogTypesAsync(CatalogDbContext _context) =>
        _context.CatalogTypes.ToListAsync();

    public static async Task<Results<Ok<List<ItemViewModel>>, BadRequest<string>>> ItemsByIdAsync(
        [FromQuery] string ids, 
        CatalogDbContext _context)
    {
        if (!string.IsNullOrEmpty(ids))
        {
            var numIds = ids.Split(',').Select(id => (Ok: int.TryParse(id, out int x), Value: x));
            if (numIds.All(nid => nid.Ok))
            {
                var idsToSelect = numIds.Select(id => id.Value);

                var items = await _context.CatalogItems
                    .Where(ci => idsToSelect.Contains(ci.Id))
                    .Select(item => new ItemViewModel(
                        item.Id,
                        item.Name,
                        item.Price,
                        item.PictureFileName))
                    .ToListAsync();

                return TypedResults.Ok(items);
            }
        }

        return TypedResults.BadRequest("Ids value is invalid. Must be comma-separated list of numbers.");
    }

    public static async Task<PaginatedItemsViewModel> ItemsAsync(
        CatalogDbContext _context,
        [FromQuery] int typeId = -1,
        [FromQuery] int brandId = -1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 0
        )
    {
        var query = (IQueryable<CatalogItem>)_context.CatalogItems;

        if (typeId > -1)
        {
            query = query.Where(ci => ci.CatalogTypeId == typeId);
        }

        if (brandId > -1)
        {
            query = query.Where(ci => ci.CatalogBrandId == brandId);
        }

        var totalItems = await query
            .LongCountAsync();

        var itemsOnPage = await query
            .OrderBy(item => item.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .Select(item => new ItemViewModel(
                item.Id,
                item.Name,
                item.Price,
                item.PictureFileName))
            .ToListAsync();

        return new PaginatedItemsViewModel(pageIndex, pageSize, totalItems, itemsOnPage);
    }
}
