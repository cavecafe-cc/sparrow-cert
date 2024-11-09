#nullable enable
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace SparrowCert;

public abstract class Log {

   public static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => {
      builder.AddConsole();
   });

   public static void Info(string tag = "", string title = "", string message = "") {
      WriteLine(tag, title, message);
   }

   public static void MaskedInfo(string tag = "", string title = "", string msgToBeMasked = "") {
      WriteLine(tag, title, Mask(msgToBeMasked));
   }

   public static string Mask(string s) {
      if (string.IsNullOrWhiteSpace(s)) {
         return s;
      }
      var len = s.Length;
      return len switch {
         < 4 => s,
         > 12 => MaskMiddle(s),
         _ => MaskRemoveMiddleWithSize(s)
      };
   }

   public static void Debug(string tag = "", string title = "", string message = "") {
      #if DEBUG
      WriteLine(tag, title, message);
      #endif
   }

   public static void Warn(string tag = "", string title = "", string message = "") {
      var currentColor = Console.ForegroundColor;
      Console.ForegroundColor = ConsoleColor.Yellow;
      WriteLine(tag, title, message);
      Console.ForegroundColor = currentColor;
   }

   public static void Error(string tag = "", string title = "", string message = "") {
      var currentColor = Console.ForegroundColor;
      WriteLine(tag, title);
      Console.ForegroundColor = ConsoleColor.DarkMagenta;
      Console.WriteLine(message);
      Console.ForegroundColor = currentColor;
   }

   public static void Catch(string tag, string func, Exception e) {
      if (e is AggregateException ae) {
         foreach (var ie in ae.InnerExceptions) {
            Catch(tag, func, ie);
         }
      }
      else {
         var now = DateTime.Now.ToString("u");
         Console.WriteLine($"{now} [{tag}] {func}: {e.Message}");
         var currentColor = Console.ForegroundColor;
         Console.ForegroundColor = ConsoleColor.Red;
         Console.WriteLine(e.StackTrace);
         Console.ForegroundColor = currentColor;
      }
   }

   private static void WriteLine(string tag = "", string title = "", string msg = "") {
      var now = "****";
      try {
         now = DateTime.Now.ToString("u");
      }
      catch (Exception e) {
         Console.Error.WriteLine(e.Message);
      }

      if (string.IsNullOrEmpty(tag)) {
         if (string.IsNullOrEmpty(title)) {
            Console.WriteLine($"{now} {msg}");
            return;
         }
         if (string.IsNullOrEmpty(msg)) {
            Console.WriteLine($"{now} {title}");
            return;
         }
         Console.WriteLine($"{now} {title}: {msg}");
      }
      else {
         if (string.IsNullOrEmpty(title)) {
            Console.WriteLine($"{now} [{tag}] {msg}");
            return;
         }
         if (string.IsNullOrEmpty(msg)) {
            Console.WriteLine($"{now} [{tag}] {title}");
            return;
         }
         Console.WriteLine($"{now} [{tag}] {title}: {msg}");
      }
   }

   public static void ShowJson(string tag, string title, object? o, bool mask = false) {
      var now = DateTime.Now.ToString("u");
      if (o == null) {
         Console.WriteLine($"{now} {tag} {title}: null\n");
         return;
      }
      var json = JsonSerializer.Serialize(o, mask ? Const.JsonMaskedOptions : Const.JsonOptions);
      Console.WriteLine($"{now} {tag} {title}:\n{json}\n");
   }

   public static void Entry(string tag, string func, object? o = null, bool mask = false) {
      if (o == null) {
         WriteLine(tag, $"{func} entry");
         return;
      }
      var json = JsonSerializer.Serialize(o, mask ? Const.JsonMaskedOptions : Const.JsonOptions);
      WriteLine(tag, $"{func} entry", json);
   }

   public static void Exit(string tag, string func, object? o = null, bool mask = false) {
      if (o == null) {
         WriteLine(tag, $"{func} exit");
         return;
      }
      var json = JsonSerializer.Serialize(o, mask ? Const.JsonMaskedOptions : Const.JsonOptions);
      WriteLine(tag, $"{func} entry", json);
   }


   #region Private

   private static string MaskRemoveMiddleWithSize(string s) {
      var sb = new StringBuilder(s.Length);
      sb.Append($"size({s.Length}): ");
      sb.Append(s[0]);
      sb.Append(s[1]);
      sb.Append(s[2]);
      sb.Append(s[3]);
      for (var i = 4; i < s.Length - 4; i++) {
         sb.Append('.');
      }
      sb.Append(s[^4]);
      sb.Append(s[^3]);
      sb.Append(s[^2]);
      sb.Append(s[^1]);
      return sb.ToString();
   }

   private static string MaskMiddle(string s) {
      var sb = new StringBuilder(s.Length);
      sb.Append(s[0]);
      sb.Append(s[1]);
      sb.Append(s[2]);
      sb.Append(s[3]);
      for (var i = 4; i < s.Length - 4; i++) {
         sb.Append('*');
      }
      sb.Append(s[^4]);
      sb.Append(s[^3]);
      sb.Append(s[^2]);
      sb.Append(s[^1]);
      return sb.ToString();
   }

   #endregion
}