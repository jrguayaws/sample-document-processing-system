using System;
using System.Web.UI;

namespace DocumentProcessor.WebForms
{
    public partial class ErrorPage : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var code = Request.QueryString["code"];

            if (code == "404")
            {
                HeadingText.Text = "404 - Page Not Found";
                MessageText.Text = "Sorry, there's nothing at this address.";
            }
            else
            {
                HeadingText.Text = "Something went wrong";
                MessageText.Text = "The error has been written to the application trace log.";
            }
        }
    }
}
