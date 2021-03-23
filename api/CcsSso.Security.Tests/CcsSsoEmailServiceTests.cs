using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Security.Tests
{
  public class CcsSsoEmailServiceTests
  {
    public class SendUserActivationLink
    {
      [Fact]
      public async Task SendUserActivationEmail_WhenProvidedData()
      {
        var emailServiceMoq = new Mock<IEmaillProviderService>();
        ApplicationConfigurationInfo applicationConfigurationInfo = new ApplicationConfigurationInfo()
        {
          EmailConfigurationInfo = new EmailConfigurationInfo()
          {
            UserActivationEmailTemplateId = "123"
          }
        };

        var service = GetCcsSsoEmailService(emailServiceMoq, applicationConfigurationInfo);
        var email = "test@yopmail.com";
        var verificationLink = "http://verify.com";
        var dataContent = new Dictionary<string, dynamic>();
        dataContent.Add("verificationlink", verificationLink);
        dataContent.Add("email", email);

        var emailInfo = new EmailInfo()
        {
          To = email,
          TemplateId = applicationConfigurationInfo.EmailConfigurationInfo.UserActivationEmailTemplateId,
          BodyContent = dataContent
        };

        await service.SendUserActivationLinkAsync(email, verificationLink);

        emailServiceMoq.Verify(a => a.SendEmailAsync(It.IsAny<EmailInfo>()));
      }
    }

    private static CcsSsoEmailService GetCcsSsoEmailService(Mock<IEmaillProviderService> emailServiceMoq, ApplicationConfigurationInfo applicationConfigurationInfo)
    {

      return new CcsSsoEmailService(emailServiceMoq.Object, applicationConfigurationInfo);
    }
  }
}
