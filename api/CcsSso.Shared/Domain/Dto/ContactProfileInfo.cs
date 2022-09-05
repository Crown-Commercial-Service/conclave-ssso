using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Domain.Dto
{  
  public class OrganisationContact
  {
    public string organisationId { get; set; }
  }
  public class UserContact
  {
    public string userId { get; set; }
  }
  public class SiteContact
  {
    public string siteId { get; set; }
  }
  public class Contact
  {
    public int contactId { get; set; }
    public string contactType { get; set; }
    public string contactValue { get; set; }
  }
  public class ContactOrgResponseInfo
  {
    public OrganisationContact detail { get; set; }
    public int contactPointId { get; set; }
    public int originalContactPointId { get; set; }
    public int assignedContactType { get; set; }
    public List<Contact> contacts { get; set; }
    public string contactPointReason { get; set; }
    public string contactPointName { get; set; }
    public string contactType { get; set; }
    public string contactId { get; set; }

  }
  public class ContactUserResponseInfo
  {
    public UserContact detail { get; set; }
    public int contactPointId { get; set; }
    public int originalContactPointId { get; set; }
    public int assignedContactType { get; set; }
    public List<Contact> contacts { get; set; }
    public string contactPointReason { get; set; }
    public string contactPointName { get; set; }
    public string contactType { get; set; }
    public string contactId { get; set; }

  }
  public class ContactSiteResponseInfo
  {
    public SiteContact detail { get; set; }
    public int contactPointId { get; set; }
    public int originalContactPointId { get; set; }
    public int assignedContactType { get; set; }
    public List<Contact> contacts { get; set; }
    public string contactPointReason { get; set; }
    public string contactPointName { get; set; }
    public string contactType { get; set; }
    public string contactId { get; set; }

  }
  public class ContactResponseInfo
  {
    public OrganisationContact detail { get; set; }
    public int contactPointId { get; set; }
    public int originalContactPointId { get; set; }
    public int assignedContactType { get; set; }
    public List<Contact> contacts { get; set; }
    public string contactPointReason { get; set; }
    public string contactPointName { get; set; }
    public string contactDeducted { get; set; }
    public string contactDedcutedId { get; set; }

    public List<ContactOrgResponseInfo> contactOrgResponseInfo { get; set; }
    public List<ContactUserResponseInfo> contactUserResponseInfo { get; set; }
    public List<ContactSiteResponseInfo> contactSiteResponseInfo { get; set; }
  }
}
