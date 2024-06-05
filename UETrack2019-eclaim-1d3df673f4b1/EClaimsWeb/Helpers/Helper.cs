using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace EClaimsWeb.Helpers
{
    public static class Helper
    {
        public static string GetModelValidationErrors(ModelStateDictionary ModelState)
        {
            StringBuilder sbErros = new StringBuilder();
            if (ModelState != null && ModelState.Values.Any())
            {
                var errorsList = ModelState.Values.SelectMany(x => x.Errors).ToList();
                foreach (var error in errorsList)
                {
                    sbErros.Append(error.ErrorMessage);
                }
            }
            return sbErros.ToString();
        }

        public static string RelativeDate(DateTime yourDate)
        {

            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.Now.Ticks - yourDate.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

            if (delta < 2 * MINUTE)
                return "a minute ago";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * MINUTE)
                return "an hour ago";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours ago";

            if (delta < 48 * HOUR)
                return "yesterday";

            if (delta < 30 * DAY)
                return ts.Days + " days ago";

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }


        public static string Month(int month)
        {
            if (month == 1)
                return "jan";
            else if (month == 2)
                return "feb";
            else if (month == 3)
                return "mar";
            else if (month == 4)
                return "apr";
            else if (month == 5)
                return "may";
            else if (month == 6)
                return "jun";
            else if (month == 7)
                return "jul";
            else if (month == 8)
                return "aug";
            else if (month == 9)
                return "sep";
            else if (month == 10)
                return "oct";
            else if (month == 11)
                return "nov";
            else 
                return "dec";

            
        }

        public static string GetFullName(this IPrincipal user)
        {
            var claim = ((ClaimsIdentity)user.Identity).FindFirst(ClaimTypes.GivenName);
            return claim == null ? null : claim.Value;
        }
    }
}
