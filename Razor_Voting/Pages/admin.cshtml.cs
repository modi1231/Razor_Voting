using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Razor_Voting.Data;

namespace Razor_Voting.Pages
{
    public class adminModel : PageModel
    {
        [TempData]
        public string Message { get; set; }// no private set b/c we need data back

        private readonly string _connection;

        private readonly IHttpContextAccessor _accessor;

        [BindProperty]
        [Display(Name = "Question")]
        public string Question { get; set; }
        
        [BindProperty]
        [Display(Name = "Expiration Date")]
        public DateTime ExpirationDate { get; set; }

        [BindProperty]
        [Display(Name = "Choices")]
        public string Choices { get; set; }

        public adminModel(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {

            _connection = configuration["ConnectionStrings:DefaultConnection"];
            _accessor = httpContextAccessor;
            Message += $" {_accessor.HttpContext.Connection.RemoteIpAddress}";

            ExpirationDate = DateTime.Now.AddDays(3);
        }


        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            POLL myPoll = new POLL
            {
                POLL_QUESTION = Question,
                EXPIRATION_DATE = ExpirationDate
            };

            string[] tempChoices = Choices.Split(Environment.NewLine);
            for (int i = 0; i < tempChoices.Length; i++)
            {
                myPoll.CHOICES.Add(new CHOICE()
                {
                     CHOICE_ID = i+1,
                      CHOICE_TEXT = tempChoices[i]
                });
            }

            DataAccess da = new DataAccess(_connection);
            await da.SaveNewPollAsync(myPoll);

            Message = "Saved";

            return RedirectToPage("admin");
        }
    }
}
