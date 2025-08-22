using Entity;
using Microsoft.EntityFrameworkCore;

namespace Provider {
    public class CosmosDbProvider: ICosmosDbProvider
    {
        private InventoryDbContext _dbContext;
        public CosmosDbProvider(InventoryDbContext dbcontext)
        {
            _dbContext = dbcontext;
        }

        public async Task AddAsync(Product product)
        {
            await _dbContext.products.AddAsync(product);
            await _dbContext.SaveChangesAsync();
        }
        public async Task DeleteAsync(DeleteRequestDTO deleteRequest)
        {
            Product product;
            if(!string.IsNullOrEmpty(deleteRequest.Name))
                product = await _dbContext.products.FirstOrDefaultAsync(p => p.Name == deleteRequest.Name && p.ProductCode == deleteRequest.ProductCode && p.Location == deleteRequest.Location);

            else 
                product = await _dbContext.products.FirstOrDefaultAsync(p => p.ProductCode == deleteRequest.ProductCode && p.Location == deleteRequest.Location);
            
            if(product is not null)
            {
                _dbContext.products.Remove(product);
                await _dbContext.SaveChangesAsync();
            }
            
        }
        public async Task UpdateAsync(UpdateRequestDTO updateRequest)
        {
            var existingProduct = await _dbContext.products.FirstOrDefaultAsync(p => p.Location == updateRequest.Location && p.ProductCode == updateRequest.ProductCode);
            if(existingProduct is not null)
            {
                if (updateRequest.ProductCode is not null) existingProduct.ProductCode = updateRequest.ProductCode;
                if (updateRequest.Subcategory is not null) existingProduct.Subcategory = updateRequest.Subcategory;
                if (updateRequest.Name is not null) existingProduct.Name = updateRequest.Name;
                if (updateRequest.Price is not null) existingProduct.Price = updateRequest.Price ?? existingProduct.Price;
                if (updateRequest.Category is not null) existingProduct.Category = updateRequest.Category;
                if (updateRequest.Description is not null) existingProduct.Description = updateRequest.Description;
                if (updateRequest.Quantity is not null) existingProduct.Quantity = updateRequest.Quantity?? existingProduct.Quantity;
                if (updateRequest.Description is not null) existingProduct.Description = updateRequest.Description;

                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task <Product> GetProduct(GetProductDTO getProductDTO)
        {
            var existingProduct = await _dbContext.products.FirstOrDefaultAsync(p => 
            p.ProductCode == getProductDTO.ProductCode &&
            p.Location == getProductDTO.Location 
            );
            return existingProduct;
        }

        public async Task<List<Product>> GetProducts(GetProductsDTO getProductDTO)
        {

            return await _dbContext.products
                .Where(p => p.Location == getProductDTO.Location && 
                (string.IsNullOrEmpty(getProductDTO.Category) || p.Category == getProductDTO.Category) &&
                (string.IsNullOrEmpty(getProductDTO.Subcategory) || p.Subcategory == getProductDTO.Subcategory))
               .ToListAsync();
        }


    }
}