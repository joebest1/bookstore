namespace WebApplication8.Data.ViewModels;
  
public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } = string.Empty;
}