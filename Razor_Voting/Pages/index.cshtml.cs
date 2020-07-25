using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Razor_Voting.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Razor_Voting.Pages
{
    public class indexModel : PageModel
    {
        [TempData]
        public string Message { get; set; }// no private set b/c we need data back


        public List<POLL> myPoll { get; set; }

        private readonly string _connection;

        private IHttpContextAccessor _accessor;

        [BindProperty]
        public string choiceSelected { get; set; }

        //for testing.
        private string clientIP = "127.0.0.1";

         public indexModel(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {

            _connection = configuration["ConnectionStrings:DefaultConnection"];
            _accessor = httpContextAccessor;

            clientIP = _accessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        public async Task OnGetAsync()
        {
            DataAccess da = new DataAccess(_connection);
            choiceSelected = "";

            myPoll = await da.LoadPoll(clientIP);

        }

        public async Task<IActionResult> OnPostAsync(string pollID)
        {
            Message = $"Selected: {choiceSelected}";

            DataAccess da = new DataAccess(_connection);
            int choice = Int32.Parse(choiceSelected);

            VotingData data = new VotingData()
            {
                IP_ADDRESS = clientIP,
                CHOICE_ID = choice,
                POLL_ID = Guid.Parse(pollID)
            };

            await da.SavePollData(data);

            return RedirectToPage("index");
        }

    }
}