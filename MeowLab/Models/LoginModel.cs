namespace MeowLab.Models
{
    public class LoginModel2
    {
        public string e_mail {  get; set; }
        public string code { get; set; }
        public LoginModel2(string e_mail, string code)
        {
            this.e_mail = e_mail;
            this.code = code;
        }
    }
}
