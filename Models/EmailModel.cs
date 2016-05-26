using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AvonessaMVC5.Models
{
    public class EmailModel
    {
        public string Subject { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string From { get; set; }

        public string To { get; set; }
        [Required]
        public string Body { get; set; }
    }
}