using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SparrowCert.Certes;
using SparrowCert.Certificates;
using SlackParams = System.Collections.Generic.Dictionary<string, string>;


namespace SparrowCert.Store;

public class Message {
   public string Channel { get; set; }
   public string SenderName { get; set; }
   public string FilePath { get; set; }
   public string Text { get; set; }
   public string Subject { get; set; }
}

public interface INotify : IDisposable {
   public Task<bool> Notify(CertType type, byte[] data);
}

public class SlackFile {
   public string id { get; set; }
   public string title { get; set; }
}

public class SlackSender(NotifyConfig.SlackConfig cfg, string domain) : INotify {
   private string _domain { get; } = domain;
   private NotifyConfig.SlackConfig _slack { get; } = cfg;

   public async Task<bool> Notify(CertType type, byte[] data) {
      try {
         var fileName = type switch {
            CertType.PrivateKey => $"{_domain}-privkey.pem",
            CertType.PfxCert => $"{_domain}.pfx",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
         };
         var subject = type switch {
            CertType.PrivateKey => "private key file uploaded",
            CertType.PfxCert => "SSL certificate file uploaded",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
         };

         var ret = await SendDataToSlack(data, fileName);

         if (type != CertType.PfxCert) return ret;
         var pems = CertUtil.CreatePemFilesFromPfx(data, _domain);
         ret = await SendDataToSlack(Encoding.UTF8.GetBytes(pems.chainPem), $"{_domain}-chain.pem");
         ret = await SendDataToSlack(Encoding.UTF8.GetBytes(pems.certPem), $"{_domain}-cert.pem");
         ret = await SendDataToSlack(Encoding.UTF8.GetBytes(pems.fullchainPem), $"{_domain}-fullchain.pem");
         return ret;
      }
      catch (Exception e) {
         Console.WriteLine($"Error uploading file to slack: {e.Message}");
      }

      return false;
   }

   public void Dispose() {
      // nothing to dispose so far
   }

   private async Task<bool> SendDataToSlack(byte[] data, string fileName) {
      using var client = new HttpClient();
      client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _slack.Token);

      #region Slack API - (GET) files.getUploadURLExternal

      var @params = new SlackParams {
         { "filename", $"{fileName}" },
         { "length", $"{data.Length}" }
      };
      var queryString = string.Join("&", @params.Select(x => $"{x.Key}={x.Value}"));
      var url = $"https://slack.com/api/files.getUploadURLExternal?{queryString}";
      var res = await client.GetAsync(url);
      var slack = await IsSlackOK(res, "files.getUploadURLExternal");
      if (!slack.ok) return false;

      #endregion

      #region Slack API (POST) - using "upload_url" from files.getUploadURLExternal

      var uploadUrl = slack.json["upload_url"].ToString();
      var fileId = slack.json["file_id"].ToString();
      using var uploadData = new ByteArrayContent(data);
      res = await client.PostAsync(uploadUrl, uploadData);
      slack = await IsSlackOK(res, "upload file");
      if (!slack.ok) return false;

      #endregion

      #region Slack API (POST) - files.completeUploadExternal

      url = "https://slack.com/api/files.completeUploadExternal";
      @params = new SlackParams {
         { "channel_id", string.Join(',', _slack.Channels) },
         { "initial_comment", _slack.Body }
      };

      var files = new SlackFile[] {
         new() { id = fileId, title = fileName }
      };
      @params.Add("files", JsonSerializer.Serialize(files));

      var encodedContent = new FormUrlEncodedContent(@params);
      res = await client.PostAsync(url, encodedContent);
      slack = await IsSlackOK(res, "files.completeUploadExternal");

      return slack.ok;

      #endregion
   }

   private async Task<(bool ok, Dictionary<string, object> json)> IsSlackOK(HttpResponseMessage res, string message) {
      if (!res.IsSuccessStatusCode || res.ReasonPhrase != "OK") {
         Console.WriteLine($"http error {message}: {res.ReasonPhrase}");
         return (false, null);
      }

      var content = await res.Content.ReadAsStringAsync();
      if (content.StartsWith("OK")) {
         return (true, null);
      }

      var slackJson = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
      if (slackJson["ok"] != null && slackJson["ok"].ToString() == "True") return (true, slackJson);
      Console.WriteLine($"slack error {message}: {slackJson["error"]}");
      return (false, null);
   }
}