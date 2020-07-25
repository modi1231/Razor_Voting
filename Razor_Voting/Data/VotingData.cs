using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Razor_Voting.Data
{
    public class VotingData
    {
        public Guid POLL_ID { get; set; }
        public string IP_ADDRESS { get; set; }
        public int CHOICE_ID { get; set; }
        public DateTime DATE_ENTERED { get; set; }
    }
}
