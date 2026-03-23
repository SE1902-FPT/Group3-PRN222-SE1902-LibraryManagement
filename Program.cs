<<<<<<< HEAD
﻿using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
=======
using Group3_SE1902_PRN222_LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;
>>>>>>> 99d6fafb21e2b4fc1398ba7a4dce645e419a3175

namespace Group3_SE1902_PRN222_LibraryManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
<<<<<<< HEAD
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });
=======
>>>>>>> 99d6fafb21e2b4fc1398ba7a4dce645e419a3175

            //Add DB Context
            builder.Services.AddDbContext<ThuVienContext>(options =>
                     options.UseSqlServer(builder.Configuration.GetConnectionString("ThuvienDB")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
<<<<<<< HEAD
            
=======
>>>>>>> 99d6fafb21e2b4fc1398ba7a4dce645e419a3175
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
