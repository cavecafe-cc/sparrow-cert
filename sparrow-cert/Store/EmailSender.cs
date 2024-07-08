using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public class EmailSender(NotifyConfig.EmailConfig email, string domain) : INotify {
   public void Dispose() {
      email = null;
   }

   public async Task<bool> Notify(CertType type, byte[] data) {
      var result = false;
      try {
         var subject = type switch {
            CertType.PrivateKey => "private key file uploaded",
            CertType.PfxCert => "SSL certificate file uploaded",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
         };
         var fileName = type switch {
            CertType.PrivateKey => $"{domain}-privkey.pem",
            CertType.PfxCert => $"{domain}-cert.pfx",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
         };
         var smtp = new SmtpClient(email.SmtpHost, email.SmtpPort) {
            Credentials = new NetworkCredential(email.SmtpUser, email.SmtpPwd),
         };
         var mail = new MailMessage(email.SenderEmail, email.Recipient) {
            Subject = subject,
            Body = email.Body,
            IsBodyHtml = email.Html
         };
         using var memoryStream = new MemoryStream(data);
         mail.Attachments.Add(new Attachment(memoryStream, fileName));
         if (type == CertType.PfxCert) {
            var pems = CertUtil.CreatePemFilesFromPfx(data, domain);

            memoryStream.Flush();
            _ = memoryStream.Read(Encoding.UTF8.GetBytes(pems.chainPem));
            mail.Attachments.Add(new Attachment(memoryStream, $"{domain}-chain.pem"));

            memoryStream.Flush();
            _ = memoryStream.Read(Encoding.UTF8.GetBytes(pems.certPem));
            mail.Attachments.Add(new Attachment(memoryStream, $"{domain}-cert.pem"));

            memoryStream.Flush();
            _ = memoryStream.Read(Encoding.UTF8.GetBytes(pems.fullchainPem));
            mail.Attachments.Add(new Attachment(memoryStream, $"{domain}-fullchain.pem"));
         }

         smtp.Send(mail);
         result = true;
      }
      catch (Exception e) {
         Console.WriteLine($"Error sending email: {e.Message}");
      }

      return await Task.FromResult(result);
   }
}