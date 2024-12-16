namespace APP.Utils;

public static class CurrencyUtils
{
    public static IEnumerable<(string Name, string Symbol, bool IsDefault)> All()
    {
        return new List<(string Name, string Symbol, bool IsDefault)>
        {
            ("Algerian Dinar", "د.ج", false),
            ("Angolan Kwanza", "Kz", false),
            ("Central African CFA Franc", "FCFA", false),
            ("Egyptian Pound", "£", false),
            ("Ghanaian Cedi", "GH₵", false),
            ("Kenyan Shilling", "KSh", false),
            ("Moroccan Dirham", "MAD", false),
            ("Nigerian Naira", "₦", false),
            ("South African Rand", "R", false),
            ("Tanzanian Shilling", "TSh", false),
            ("Zambian Kwacha", "ZK", false),
            ("Zimbabwean Dollar", "Z$", false),
            ("Tunisian Dinar", "د.ت", false),
            ("Libyan Dinar", "LD", false),
            ("Mozambican Metical", "MT", false),
            ("Botswana Pula", "P", false),
            ("Namibian Dollar", "$", false),
            ("Ugandan Shilling", "USh", false),
            ("West African CFA Franc", "CFA", false),
            ("Ethiopian Birr", "Br", false),
            ("US Dollar", "$", true),
            ("Euro", "€", false),
            ("British Pound", "£", false),
            ("Australian Dollar", "A$", false)
        };
    }
}