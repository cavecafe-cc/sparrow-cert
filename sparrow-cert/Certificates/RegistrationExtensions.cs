using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SparrowCert.Certes;
using SparrowCert.Certificates;
using SparrowCert.Store;


[assembly: InternalsVisibleTo("SparrowCert.Tests")]

namespace SparrowCert;

public static class RegistrationExtensions {
   private static void AddSparrowCertStore(
      this IServiceCollection services) {
      if (services.Any(x => x.ServiceType == typeof(IStore)))
         return;

      services.AddSingleton<IStore, Store.Store>();
   }

   public static void AddSparrowCertRenewalHook<TRenewalHook>(this IServiceCollection services) where TRenewalHook : class, IRenewalHook {
      services.AddSparrowCertStore();
      services.AddSingleton<IRenewalHook, TRenewalHook>();
   }

   public static void AddSparrowCertCertificateStore(
      this IServiceCollection services,
      bool isStaging,
      Func<CertType, byte[], Task> onSave,
      Func<CertType, Task<byte[]>> onLoad,
      Func<CertType, byte[], Task<bool>> onNotify) {
      AddSparrowCertCertificateStore(services,
         new CertStore(
            isStaging,
            onSave,
            onLoad,
            onNotify));
   }

   private static void AddSparrowCertCertificateStore(
      this IServiceCollection services,
      ICertStore certStore) {
      AddSparrowCertCertificateStore(services,
         (_) => certStore);
   }

   private static void AddSparrowCertCertificateStore(
      this IServiceCollection services,
      Func<IServiceProvider, ICertStore> certStoreFactory) {
      services.AddSparrowCertStore();
      services.AddSingleton(certStoreFactory);
   }

   public static void AddSparrowCertFileCertStore(this IServiceCollection services,
                                                  NotifyConfig notify,
                                                  bool isStaging,
                                                  string basePath,
                                                  string filePrefix) {
      AddSparrowCertCertificateStore(services,
         new FileCertStore(notify, isStaging, basePath, filePrefix + (isStaging ? ".staging" : "")));
   }

   public static void AddSparrowCertChallengeStore(
      this IServiceCollection services,
      bool isStaging,
      OnStoreChallenges onStore,
      OnLoadChallenges onLoad,
      OnDeleteChallenges onDelete) {
      AddSparrowCertChallengeStore(services,
         new ChallengeStore(onStore,
            onLoad,
            onDelete));
   }

   private static void AddSparrowCertChallengeStore(
      this IServiceCollection services,
      IChallengeStore challengeStore) {
      AddSparrowCertChallengeStore(services,
         (_) => challengeStore);
   }

   private static void AddSparrowCertChallengeStore(
      this IServiceCollection services,
      Func<IServiceProvider, IChallengeStore> certStoreFactory) {
      services.AddSparrowCertStore();
      services.AddSingleton(certStoreFactory);
   }

   public static void AddSparrowCertFileChallengeStore(
      this IServiceCollection services,
      bool isStaging,
      string storePath,
      string filePrefix) {
      AddSparrowCertChallengeStore(services,
         new FileChallengeStore(storePath, filePrefix + (isStaging ? ".staging" : "")));
   }

   public static void AddSparrowCertMemoryChallengeStore(
      this IServiceCollection services) {
      AddSparrowCertChallengeStore(
         services,
         new MemoryChallengeStore());
   }

   public static void AddSparrowCertMemoryCertStore(
      this IServiceCollection services) {
      AddSparrowCertCertificateStore(
         services,
         new MemoryCertStore());
   }

   public static void AddSparrowCert(
      this IServiceCollection services, CertConfiguration config) {
      services.AddTransient<IConfigureOptions<KestrelServerOptions>, RenewalOptions>();
      services.AddSparrowCertStore();

      var hostName = CertUtil.GetDomainOrHostname(config.Domains.First());
      services.AddSparrowCertFileChallengeStore(config.UseStaging, config.KeyStorePath, hostName);
      services.AddSingleton(config);

      services.AddSingleton<ILetsEncryptClientFactory, LetsEncryptClientFactory>();
      services.AddSingleton<ICertValidator, CertValidator>();
      services.AddSingleton<ICertProvider, CertProvider>();
      services.AddTransient<IRenewalService, RenewalService>();
      services.AddTransient<IHostedService, RenewalService>();
   }

   public static void UseSparrowCert(
      this IApplicationBuilder app) {
      app.UseMiddleware<ChallengeApprovalMiddleware>();
   }

   public static void AddSparrowCertRenewalHook(this IServiceCollection services, NotifyConfig notify, IEnumerable<string> domains) {
      services.AddSingleton<IRenewalHook, RenewalHook>(x => new RenewalHook(notify, domains.ToArray()));
   }
}