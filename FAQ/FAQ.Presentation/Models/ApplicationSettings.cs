namespace FAQ.Presentation.Models
{
    public class ApplicationSettings
    {
        public string JWT_Secret { get; set; }
        public string JWT_Issuer { get; set; }
        public string JWT_Audience { get; set; }
        public string Client_URL { get; set; }
    }
}
