using Microsoft.Extensions.DependencyInjection;
using Zpravodajstvi.Application.Interfaces;
using Zpravodajstvi.Application.Services;

namespace Zpravodajstvi.Application;

// Registrace aplikačních služeb do DI (volá se z Program.cs)
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ITagService, TagService>();
        return services;
    }
}
