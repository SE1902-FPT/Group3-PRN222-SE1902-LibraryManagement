using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Group3_SE1902_PRN222_LibraryManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Add DB Context
            builder.Services.AddDbContext<ThuVienContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ThuvienDB")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            // Lưu ý: Phải có UseAuthentication TRƯỚC UseAuthorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}