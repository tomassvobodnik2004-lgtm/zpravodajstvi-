using Zpravodajstvi.Application.DTOs;
using Zpravodajstvi.Domain.Entities;

namespace Zpravodajstvi.Application.Services;

// Převod entit na DTO, který sdílí ArticleService a CommentService.
// Jméno autora se bere z navigační vlastnosti (Article.Author, Comment.User), repozitáře ji načítají přes Include.
internal static class DtoMapper
{
    public const string UnknownAuthor = "Neznámý autor";

    public static ArticleListDto ToListDto(Article article)
    {
        return new ArticleListDto
        {
            Id = article.Id,
            Title = article.Title,
            Perex = article.Perex,
            CategoryId = article.CategoryId,
            CategoryName = article.Category?.Name ?? "Bez kategorie",
            AuthorName = article.Author?.DisplayName ?? UnknownAuthor,
            CreatedAt = article.CreatedAt,
            ImageUrl = article.Images?.FirstOrDefault()?.Url,
            CommentsCount = article.Comments?.Count ?? 0,
            Tags = article.Tags?.Select(t => t.Name).ToList() ?? new List<string>()
        };
    }

    public static CommentDto ToCommentDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            // Aktuální jméno uživatele; komentář bez uživatele (smazaný účet, starý záznam) má jen uložené jméno
            AuthorName = comment.User?.DisplayName ?? comment.AuthorName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            ArticleId = comment.ArticleId,
            ArticleTitle = comment.Article?.Title ?? string.Empty,
            UserId = comment.UserId
        };
    }
}
