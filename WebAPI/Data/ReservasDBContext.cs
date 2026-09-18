using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

namespace WebAPI.Data
{
    public class ReservasDBContext : DbContext
    {
        public ReservasDBContext(DbContextOptions<ReservasDBContext> options) : base(options){ }
        //DbSet es una "Representacion" de las tablas de BD indicandoles el tipo que deberian ser segun el codigo
        public DbSet<Usuario> Usuarios{ get; set; }
        public DbSet<Rol> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //mapear las tablas de la BD para asegurarnos que sean iguales segun la entidad
            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Rol>().ToTable("Roles");

            modelBuilder.Entity<Usuario>().HasOne(u => u.Rol).WithMany().HasForeignKey(u => u.RolId);

        }
    }
}
