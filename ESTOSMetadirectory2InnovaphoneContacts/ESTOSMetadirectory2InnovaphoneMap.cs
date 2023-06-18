using CsvHelper.Configuration;
using System.Globalization;

namespace ESTOSMetadirectory2InnovaphoneContacts
{
    public sealed class ESTOSMetadirectory2InnovaphoneMap : ClassMap<InnovaphoneContact>
    {
        public ESTOSMetadirectory2InnovaphoneMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.givenname).Name("Nachname");
            Map(m => m.sn).Name("Vorname");
            Map(m => m.company).Name("Unternehmen");
            Map(m => m.displayname).Name("Angezeigter Name");
            Map(m => m.email).Name("E-Mail");
            Map(m => m.telephonenumber).Name("Tel. Geschäftlich");
            Map(m => m.mobile).Name("Tel. Mobil");
            Map(m => m.homephone).Name("Tel. Geschäftlich");
            Map(m => m.facsimiletelephonenumber).Name("Fax gesch.");
            Map(m => m.city).Name("Ort");
            Map(m => m.street).Name("Straße");
            Map(m => m.postalcode).Name("PLZ");
            Map(m => m.state).Name("Bundesland");
            Map(m => m.country).Name("Land");
            Map(m => m.privatecity).Name("Ort privat");
            Map(m => m.privatestreet).Name("Straße privat");
            Map(m => m.privatepostalcode).Name("PLZ privat");
            Map(m => m.privatestate).Name("Bundesland privat");
            Map(m => m.privatecountry).Name("Land privat");
            Map(m => m.title).Name("Benutzer 8");
            Map(m => m.position).Name("Position");
            Map(m => m.department).Name("Abteilung");
            Map(m => m.description).Name("Bemerkung");
            Map(m => m.roomnumber).Name("Benutzer 9");
            Map(m => m.info).Name("Kundennr");
            Map(m => m.url).Name("Webseite");
            Map(m => m.sip).Name("Benutzer 10");
        }
    }
}
