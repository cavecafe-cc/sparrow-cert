using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using SparrowCert.Certes;
using SparrowCert.Certificates;

namespace SparrowCert.Store;

public class EmailSender(NotifyConfig.EmailConfig email, string hostname) : INotify {
   private const string tag = nameof(EmailSender);
   public void Dispose() {
      email = null;
   }

   public async Task<bool> Notify(CertType type, byte[] data) {
      if (!email.Enabled) {
         Log.Warn(tag, nameof(Notify), "Email notifications are disabled");
         return false;
      }
      var result = false;
      try {
         var subject = type switch {
            CertType.PrivateKey => "private key file uploaded",
            CertType.PfxCert => "SSL certificate file uploaded",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
         };
         var fileName = type switch {
            CertType.PrivateKey => $"{hostname}-privkey.pem",
            CertType.PfxCert => $"{hostname}.pfx",
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
            var pems = CertUtil.CreatePemFilesFromPfx(data, hostname);

            memoryStream.Flush();
            _ = memoryStream.Read(Encoding.UTF8.GetBytes(pems.chainPem));
            mail.Attachments.Add(new Attachment(memoryStream, $"{hostname}-chain.pem"));

            memoryStream.Flush();
            _ = memoryStream.Read(Encoding.UTF8.GetBytes(pems.certPem));
            mail.Attachments.Add(new Attachment(memoryStream, $"{hostname}-cert.pem"));

            memoryStream.Flush();
            _ = memoryStream.Read(Encoding.UTF8.GetBytes(pems.fullchainPem));
            mail.Attachments.Add(new Attachment(memoryStream, $"{hostname}-fullchain.pem"));
         }

         smtp.Send(mail);
         result = true;
      }
      catch (Exception e) {
         Log.Catch(tag, "Error sending email", e);
      }

      return await Task.FromResult(result);
   }
}