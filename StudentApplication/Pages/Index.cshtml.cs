using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace StudentApplication.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public ApplicationForm Form { get; set; } = new();

    public bool IsSubmitted { get; private set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        IsSubmitted = true;
    }

    public class ApplicationForm
    {
        [Required(ErrorMessage = "Укажите имя.")]
        [Display(Name = "Имя и фамилия")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите электронную почту.")]
        [EmailAddress(ErrorMessage = "Введите корректный адрес электронной почты.")]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите дату рождения.")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата рождения")]
        public DateTime? BirthDate { get; set; }

        [Required(ErrorMessage = "Укажите город.")]
        [Display(Name = "Город")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Расскажите немного о себе")]
        public string About { get; set; } = string.Empty;
    }
}
