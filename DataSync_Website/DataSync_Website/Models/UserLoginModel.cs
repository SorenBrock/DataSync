using System.ComponentModel.DataAnnotations;

namespace DataSync_Website.Models
{
    public class UserLoginModel
    {
        [Required(ErrorMessage = "Indtast brugernavn")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Du må kun bruge a-z / 0-9 i brugernavn")]
        public string UserName { get; set; }
     
        [Required(ErrorMessage = "Indtast password")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Du må kun bruge a-z / 0-9 i password")]
        public string UserPassword { get; set; }

        public bool RememberMe { get; set; }

    }   
}