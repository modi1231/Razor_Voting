using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Razor_Voting.Data
{

    public class POLL
    {
        public Guid POLL_ID { get; set; }
        
        [Display(Name = "Question")]
        public string POLL_QUESTION { get; set; }

        public List<CHOICE> CHOICES { get; set; }

        public bool BEEN_ANSWERED { get; set; }

        public DateTime DATE_ENTERED { get; set; }

        [Display(Name = "Expires")]
        public DateTime EXPIRATION_DATE { get; set; }

        public POLL()
        {
            CHOICES = new List<CHOICE>();
        }
    }

    public class CHOICE
    {
        public int CHOICE_ID { get; set; }

        public string CHOICE_TEXT { get; set; }

        public int COUNT { get; set; }

        public double PERCENTAGE { get; set; }

        public bool USER_PICKED { get; set; }
    }
}
