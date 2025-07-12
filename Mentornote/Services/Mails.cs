#nullable disable
using System.Net;
using System.Net.Mail;

namespace Mentornote.Services
{
    public class Mails
    {
        private string password;
        private string smtpServer;
        private int port;
        private string fromEmail;
        public string SendEmail(string toEmailAddress, string messageBody, string messageSubject)
        {
            string success;
            try
            {
                // Create the email client object
                using (SmtpClient client = new SmtpClient(smtpServer))
                {
                    client.Port = 587;
                    client.EnableSsl = true;


                    client.Credentials = new NetworkCredential(fromEmail, password);  //Ezra


                    // Create the email message
                    MailMessage message = new MailMessage(fromEmail, toEmailAddress);
                    message.Subject = messageSubject;
                    message.Body = messageBody;


                    // Send the email
                    client.Send(message);
                    success = "Successful.";

                }
            }
            catch (Exception ex)
            {
                success = $"Error sending email: {ex.Message}";
            }

            return success;
        }
    }
}
