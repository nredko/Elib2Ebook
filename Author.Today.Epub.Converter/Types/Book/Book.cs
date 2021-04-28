using System.Collections.Generic;
using Author.Today.Epub.Converter.Logic;
using EpubSharp.Format;

namespace Author.Today.Epub.Converter.Types.Book {
    public class Book {
        public Book(long id) {
            Id = id;
        }
        
        /// <summary>
        /// Идентификатор книги
        /// </summary>
        public long Id;
        
        /// <summary>
        /// Название книги
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Автор книги
        /// </summary>
        public string Author { get; set; }
        
        /// <summary>
        /// Обложка
        /// </summary>
        public Image Cover;

        public string FullName => $"{Id}. {Author} - {Title}";
        
        /// <summary>
        /// Части
        /// </summary>
        public IEnumerable<Chapter> Chapters { get; set; }

        /// <summary>
        /// Сохранение книги
        /// </summary>
        /// <param name="savePath">Путь для сохранения</param>
        /// <param name="resourcesPath">Путь к папке с ресурсами</param>
        public void Save(string savePath, string resourcesPath) {
            EpubBuilder.Create()
                .AddAuthor(Author)
                .WithTitle(Title)
                .WithCover(Cover)
                .WithFiles(resourcesPath, "*.ttf", EpubContentType.FontTruetype)
                .WithFiles(resourcesPath, "*.css", EpubContentType.Css)
                .WithChapters(Chapters)
                .Build(savePath, FullName);
        }
    }
}
