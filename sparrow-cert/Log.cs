#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SparrowCert;

public class Log {

   private abstract class Const {
      public static readonly JsonSerializerOptions JsonOptions = new() {
         IgnoreReadOnlyFields = true,
         DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
         WriteIndented = true
      };

   }

   public static void Info(string tag = "", string title = "", string message = "") {
      WriteLine(tag, title, message);
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

   private static void WriteLine(string tag = "", string func = "", string msg = "") {
      var now = "****";
      try {
         now = DateTime.Now.ToString("u");
      }
      catch (Exception e) {
         Console.Error.WriteLine(e.Message);
      }

      if (string.IsNullOrEmpty(tag)) {
         if (string.IsNullOrEmpty(func)) {
            Console.WriteLine($"{now} {msg}");
            return;
         }
         Console.WriteLine($"{now} {func}: {msg}");
      }
      else {
         if (string.IsNullOrEmpty(func)) {
            Console.WriteLine($"{now} [{tag}] {msg}");
            return;
         }
         Console.WriteLine($"{now} [{tag}] {func}: {msg}");
      }
   }

   public static void ShowJson(string tag, string title, object? o) {
      var now = DateTime.Now.ToString("u");
      if (o == null) {
         Console.WriteLine($"{now} {tag} {title}: null\n");
         return;
      }

      Console.WriteLine($"{now} {tag} {title}:\n{JsonSerializer.Serialize(o, Const.JsonOptions)}\n");
   }

}