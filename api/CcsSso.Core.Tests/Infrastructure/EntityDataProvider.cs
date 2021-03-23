using CcsSso.DbModel.Entity;

namespace CcsSso.Core.Tests.Infrastructure
{
  internal static class EntityDataProvider
  {
    public static User GetUser(int id, string fName, string lName)
    {
      return new User()
      {
        Id = id,
        // FirstName = fName,
        // SurName = lName
      };
    }

    public static Party GetParty(int id, int partyTypeId)
    {
      return new Party()
      {
        Id = id,
        PartyTypeId = partyTypeId
      };
    }

    public static PartyType GetPartyType(int id, string PartyTypeName)
    {
      return new PartyType()
      {
        Id = id,
        PartyTypeName = PartyTypeName
      };
    }
  }
}
