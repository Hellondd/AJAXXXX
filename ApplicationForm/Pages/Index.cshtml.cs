using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ApplicationForm.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public ApplicationRequest FormData { get; set; } = new();

    public bool IsSubmitted { get; private set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        IsSubmitted = ModelState.IsValid;
    }
}

public class ApplicationRequest
{
    [Required(ErrorMessage = "Укажите имя.")]
    [Display(Name = "Имя")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите фамилию.")]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес электронной почты.")]
    [EmailAddress(ErrorMessage = "Введите корректный адрес электронной почты.")]
    [Display(Name = "Электронная почта")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите город.")]
    [Display(Name = "Город")]
    public string City { get; set; } = string.Empty;

    [Range(14, 100, ErrorMessage = "Возраст должен быть от 14 до 100 лет.")]
    [Display(Name = "Возраст")]
    public int Age { get; set; }

    [Display(Name = "О себе")]
    public string About { get; set; } = string.Empty;
}
