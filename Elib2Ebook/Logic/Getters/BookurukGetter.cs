using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.Common;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class BookurukGetter : GetterBase{
    public BookurukGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://bookuruk.com/");

    protected override string GetId(Uri url) {
        return url.GetSegment(2);
    }

    public override async Task<Book> Get(Uri url) {
        url = await GetMainUrl(url);
        url = SystemUrl.MakeRelativeUri($"/book/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        
        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1"),
            Author = GetAuthor(doc),
            Annotation = doc.QuerySelector("div.single-prew__desc_anot b + p")?.InnerHtml
        };
            
        return book;
    }

    private Author GetAuthor(HtmlDocument doc) {
        var a = doc.QuerySelector("a.bookcard_author");
        var href = a.Attributes["href"]?.Value;
        return string.IsNullOrWhiteSpace(href) ? 
            new Author(a.GetText()) : 
            new Author(a.GetText(), SystemUrl.MakeRelativeUri(href));
    }

    private async Task<Uri> GetMainUrl(Uri url) {
        if (url.GetSegment(1) == "book") {
            return url;
        }

        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return url.MakeRelativeUri(doc.QuerySelector("div.content.aside-right li a").Attributes["href"].Value);
    }

    private async Task<IEnumerable<UrlChapter>> GetToc(HtmlDocument doc, Uri url) {
        var internalId = SystemUrl.MakeRelativeUri(doc.QuerySelectorAll("div.content.aside-right li a")[1].Attributes["href"].Value).GetSegment(2);
        doc = await Config.Client.GetHtmlDocWithTriesAsync(SystemUrl.MakeRelativeUri($"/book/chapters/{internalId}"));
        
        var result = doc
            .QuerySelectorAll("ul.read-chapters-list a")
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText()))
            .ToList();
        
        return SliceToc(result);
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri uri) {
        var result = new List<Chapter>();

        foreach (var urlChapter in await GetToc(doc, uri)) {
            var chapter = new Chapter {
                Title = urlChapter.Title
            };

            Console.WriteLine($"Загружаю главу {urlChapter.Title.CoverQuotes()}");

            var chapterDoc = await GetChapter(urlChapter.Url);
            if (chapterDoc != default) {
                chapter.Images = await GetImages(chapterDoc, urlChapter.Url);
                chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            }
            
            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        return doc.QuerySelector("div.read-area").InnerHtml.AsHtmlDoc();
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.single-prew__img img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}