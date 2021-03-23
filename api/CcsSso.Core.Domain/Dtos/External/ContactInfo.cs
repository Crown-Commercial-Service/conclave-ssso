using System.Collections.Generic;

namespace CcsSso.Domain.Dtos.External
{
  public class ContactInfo
  {
    public string ContactReason { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string Fax { get; set; }

    public string WebUrl { get; set; }
  }

  public class ContactResponseInfo : ContactInfo
  {
    public int ContactId { get; set; }
  }

  public class OrganisationContactInfo : ContactResponseInfo
  {
    public string OrganisationId { get; set; }
  }

  public class OrganisationContactInfoList
  {
    public string OrganisationId { get; set; }

    public List<ContactResponseInfo> ContactsList { get; set; }
  }

  public class UserContactInfo : ContactResponseInfo
  {
    public string UserId { get; set; }

    public string OrganisationId { get; set; }
  }

  public class UserContactInfoList
  {
    public string UserId { get; set; }

    public string OrganisationId { get; set; }

    public List<ContactResponseInfo> ContactsList { get; set; }
  }

  public class OrganisationSiteContactResponse : ContactResponseInfo
  {
    public int SiteId { get; set; }
  }

  public class OrganisationSiteContactInfo : OrganisationSiteContactResponse
  {
    public string OrganisationId { get; set; }
  }

  public class OrganisationSiteContactInfoList
  {
    public string OrganisationId { get; set; }

    public int SiteId { get; set; }

    public List<ContactResponseInfo> SiteContacts { get; set; }
  }

  public class ContactReasonInfo
  {
    public string Key { get; set; }
    public string Value { get; set; }
  }
}
