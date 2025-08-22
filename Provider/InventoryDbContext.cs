using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entity;
using Microsoft.Extensions.Configuration;

namespace Provider
{
    public class InventoryDbContext: DbContext
    {
        private readonly IConfiguration _configuration;
        public InventoryDbContext(DbContextOptions options, IConfiguration configuration): base(options)
        {
            _configuration = configuration;
        }
        public DbSet<Product> products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            var containerName = _configuration.GetValue<string>("ContainerName");
            modelBuilder.Entity<Product>()
                .ToContainer(containerName)
                .HasPartitionKey(p => p.Location)
                .HasKey(p => p.ProductCode);
           
            modelBuilder.Entity<Product>()
                .Property(p => p.ProductCode)
                .ToJsonProperty("id");

            modelBuilder.Entity<Product>()
                .Property(p => p.ETag)
                .ToJsonProperty("_etag")
                .IsETagConcurrency();

            base.OnModelCreating(modelBuilder);
        }
    }
}
