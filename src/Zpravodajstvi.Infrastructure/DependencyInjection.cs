using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Domain.Entities;
using Zpravodajstvi.Domain.Interfaces;
using Zpravodajstvi.Infrastructure.Data;
using Zpravodajstvi.Infrastructure.Files;
using Zpravodajstvi.Infrastructure.Identity;
using Zpravodajstvi.Infrastructure.Repositories;

namespace Zpravodajstvi.Infrastructure;

// Registrace infrastruktury do DI (volá se z Program.cs): DbContext, ASP.NET Core Identity,
// repozitáře a implementace služeb pro účty, role a soubory
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // 1. Databáze SQLite
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // 2. ASP.NET Core Identity s rolemi
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 3. Repozitáře: obecný Repository<T> pro libovolnou entitu a specifické repozitáře
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        // 4. Služby, které potřebují Identity (UserManager, SignInManager, RoleManager) nebo souborový systém
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
