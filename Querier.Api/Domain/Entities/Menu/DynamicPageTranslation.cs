public class DynamicPageTranslation
{
    public int Id { get; set; }
    public int DynamicPageId { get; set; }
    public string LanguageCode { get; set; }
    public string Name { get; set; }
    public virtual DynamicPage DynamicPage { get; set; }
} 
