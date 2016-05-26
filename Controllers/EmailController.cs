using ActionMailer.Net.Mvc;
using AvonessaMVC5.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AvonessaMVC5.Controllers
{
    public class EmailController : MailerBase
    {
        public EmailResult SendEmail(EmailModel model)
        {
            To.Add("info@avonessa.com");
            From    = model.From;
            //Subject = model.Subject;
            Subject = "Вопрос от посетителя сайта";

            return Email("SendEmail", model);
        }
    }
}