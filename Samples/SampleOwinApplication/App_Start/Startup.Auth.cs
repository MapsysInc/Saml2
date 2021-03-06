﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using SampleOwinApplication.Models;
using Sustainsys.Saml2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Metadata;
using Sustainsys.Saml2.Owin;
using Sustainsys.Saml2.WebSso;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Web.Hosting;

namespace SampleOwinApplication
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            app.UseSaml2Authentication(CreateSaml2Options());
        }

        private static Saml2AuthenticationOptions CreateSaml2Options()
        {
            SPOptions spOptions = CreateSPOptions();
            Saml2AuthenticationOptions Saml2Options = new Saml2AuthenticationOptions(false)
            {
                SPOptions = spOptions,

            };

            //var idp = new IdentityProvider(new EntityId("https://sts.windows.net/e1413b17-c8b7-4388-99f0-2f613124050c/"), spOptions)
            //    {
            //        AllowUnsolicitedAuthnResponse = true,
            //        Binding = Saml2BindingType.HttpRedirect,
            //        SingleSignOnServiceUrl = new Uri("https://login.microsoftonline.com/e1413b17-c8b7-4388-99f0-2f613124050c/saml2")

            //    };

            //idp.SigningKeys.AddConfiguredKey(
            //    new X509Certificate2(
            //        HostingEnvironment.MapPath(
            //            "~/App_Data/SAMLThing.cer")));

            //Saml2Options.IdentityProviders.Add(idp);

            // It's enough to just create the federation and associate it
            // with the options. The federation will load the metadata and
            // update the options with any identity providers found.

            //            new Federation("https://login.microsoftonline.com/e1413b17-c8b7-4388-99f0-2f613124050c/federationmetadata/2007-06/federationmetadata.xml?appid=6d137192-dc97-42d0-a651-ce32b2804c33", true, Saml2Options);
            new Federation(@"~/odx_fedmyohio_idp_federation_metadata.xml", true, Saml2Options);
            return Saml2Options;
        }

        private static SPOptions CreateSPOptions()
        {
            string english = "en-us";

            Organization organization = new Organization();
            organization.Names.Add(new LocalizedName("OhioPublicDefender", english));
            organization.DisplayNames.Add(new LocalizedName("OhioPublicDefender", english));
            organization.Urls.Add(new LocalizedUri(new Uri("http://online.opd.ohio.gov"), english));

            SPOptions spOptions = new SPOptions
            {

                EntityId = new EntityId("http://localhost:57294/Saml2"),
                ReturnUrl = new Uri("https://localhost:44332/Account/ExternalLoginCallback"),
                //   DiscoveryServiceUrl = new Uri("http://localhost:52071/DiscoveryService"),
                Organization = organization,
                NameIdPolicy = new Sustainsys.Saml2.Saml2P.Saml2NameIdPolicy(true, Sustainsys.Saml2.Saml2P.NameIdFormat.EmailAddress)
            };

            ContactPerson techContact = new ContactPerson
            {
                Type = ContactType.Technical
            };
            techContact.EmailAddresses.Add("Saml2@example.com");
            spOptions.Contacts.Add(techContact);

            ContactPerson supportContact = new ContactPerson
            {
                Type = ContactType.Support
            };
            supportContact.EmailAddresses.Add("support@example.com");
            spOptions.Contacts.Add(supportContact);

            AttributeConsumingService attributeConsumingService = new AttributeConsumingService
            {
                IsDefault = true,
                ServiceNames = { new LocalizedName("Saml2", "en") }
            };

            attributeConsumingService.RequestedAttributes.Add(
                new RequestedAttribute("email")
                {
                    FriendlyName = "email",
                    IsRequired = true,
                    NameFormat = RequestedAttribute.AttributeNameFormatUnspecified
                });

            attributeConsumingService.RequestedAttributes.Add(
                new RequestedAttribute("Minimal"));

            spOptions.AttributeConsumingServices.Add(attributeConsumingService);


            spOptions.ServiceCertificates.Add(new X509Certificate2(
                AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/App_Data/Sustainsys.Saml2.Tests.pfx"));


            spOptions.MinIncomingSigningAlgorithm = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            return spOptions;
        }
    }
}