namespace Querier.Api.Models.Enums
{
    public static class ReportRenderingExtensions
    {
        public static string GetRDLCRendering(this ReportRendering me)
        {
            switch (me)
            {
                default:
                case ReportRendering.Excel:
                    return "EXCELOPENXML";
                case ReportRendering.Excel2003:
                    return "Excel";
                case ReportRendering.TIFF:
                    return "IMAGE";
                case ReportRendering.PDF:
                    return "PDF";
                case ReportRendering.Word2003:
                    return "WORD";
                case ReportRendering.Word:
                    return "WORDOPENXML";
                case ReportRendering.MHTML:
                    return "MHTML";
                case ReportRendering.HTML4:
                    return "HTML4.0";
                case ReportRendering.HTML5:
                    return "HTML5";
            }
        }

        public static string GetName(this ReportRendering me)
        {
            switch (me)
            {
                case ReportRendering.Excel2003:
                    return "Excel 2003";
                default:
                case ReportRendering.Excel:
                    return "Excel";
                case ReportRendering.TIFF:
                    return "TIFF";
                case ReportRendering.PDF:
                    return "PDF";
                case ReportRendering.Word2003:
                    return "Word 2003";
                case ReportRendering.Word:
                    return "Word";
                case ReportRendering.MHTML:
                    return "MHTML";
                case ReportRendering.HTML4:
                    return "HTML 4.0";
                case ReportRendering.HTML5:
                    return "HTML 5";
            }
        }

        public static string GetFileExtension(this ReportRendering me)
        {
            switch (me)
            {
                case ReportRendering.Excel2003:
                    return "xls";
                default:
                case ReportRendering.Excel:
                    return "xlsx";
                case ReportRendering.TIFF:
                    return "tiff";
                case ReportRendering.PDF:
                    return "pdf";
                case ReportRendering.Word2003:
                    return "doc";
                case ReportRendering.Word:
                    return "docx";
                case ReportRendering.MHTML:
                    return "mhtml";
                case ReportRendering.HTML4:
                    return "html";
                case ReportRendering.HTML5:
                    return "html";
            }
        }
    }
}
