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

public class RoyalRoadGetter : GetterBase {
    public RoyalRoadGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://royalroad.com/");

    protected override string GetId(Uri url) => url.GetSegment(2);

    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/fiction/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1[property=name]"),
            Author = GetAuthor(doc, url),
            Annotation = doc.QuerySelector("div[property=description]")?.InnerHtml,
        };
            
        return book;
    }
    
    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var bookChapter in GetToc(doc, url)) {
            var chapter = new Chapter();
            Console.WriteLine($"Загружаю главу {bookChapter.Title.CoverQuotes()}");
            
            var chapterDoc = await GetChapter(bookChapter.Url);
            
            chapter.Title = bookChapter.Title;
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml.HtmlDecode();

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(Uri url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);
        var content = doc.QuerySelector("div.chapter-content");

        foreach (var node in content.QuerySelectorAll("p, span")) {
            node.Attributes.RemoveAll();
        }

        return content.InnerHtml.AsHtmlDoc();
    }

    private IEnumerable<UrlChapter> GetToc(HtmlDocument doc, Uri url) {
        var result = doc
            .QuerySelectorAll("tr.chapter-row")
            .Select(r => r.QuerySelector("a[href^=/fiction/]"))
            .Select(a => new UrlChapter(url.MakeRelativeUri(a.Attributes["href"].Value), a.GetText()))
            .ToList();
        
        return SliceToc(result);
    }

    private static Author GetAuthor(HtmlDocument doc, Uri url) {
        var a = doc.QuerySelector("h4[property=author] span[property=name] a");
        return new Author(a.GetText(), url.MakeRelativeUri(a.Attributes["href"].Value));
    }

    private Task<Image> GetCover(HtmlDocument doc, Uri uri) {
        var imagePath = doc.QuerySelector("div.cover-art-container img")?.Attributes["src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(uri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}