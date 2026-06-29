namespace Osmos.Core.Helpers.Models
{
    public class HeaderOptions
    {
        public string PageCount { get; set; }
        public string PageNumber { get; set; }
        public string PageSize { get; set; }
    }

    public class ValueOptions
    {
        public int PageCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class PaginationOptions
    {
        public HeaderOptions HeaderOptions { get; set; }
        public ValueOptions ValueOptions { get; set; }
    }
}
