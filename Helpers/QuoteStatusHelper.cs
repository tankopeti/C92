using System.Collections.Generic;

namespace Cloud9_2.Helpers
{
    public static class QuoteStatusHelper
    {
        public static Dictionary<string, string> GetStatusDisplay()
        {
            return new Dictionary<string, string>
            {
                { "Folyamatban", "Folyamatban" },
                { "Felfüggesztve", "Felfüggesztve" },
                { "Jóváhagyásra_vár", "Jóváhagyásra vár" },
                { "Jóváhagyva", "Jóváhagyva" },
                { "Kiküldve", "Kiküldve" },
                { "Elfogadva", "Elfogadva" },
                { "Megrendelve", "Megrendelve" },
                { "Teljesítve", "Teljesítve" },
                { "Lezárva", "Lezárva" }
            };
        }
    }
}