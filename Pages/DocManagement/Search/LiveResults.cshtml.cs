// Névtér az alkalmazás struktúráján belüli kódszervezéshez.
// Ez a kód a Pages/DocManagement/Search mappában található a Razor Pages útválasztáshoz.
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Pages.DocManagement.Search
{
    // A LiveResultsModel osztály a PageModel-ből örököl, amely alapvető funkcionalitást biztosít a Razor Pages számára.
    // Ez az osztály kezeli a LiveResults Razor oldal backend logikáját, kifejezetten a dokumentumok keresésére.
    public class LiveResultsModel : PageModel
    {
        // Privát, csak olvasható mező az adatbázis kontextus tárolására.
        // Ezt függőséginjektálással kapjuk meg, és az adatbázis lekérdezésére használjuk.
        private readonly ApplicationDbContext _context;

        // Konstruktor a függőséginjektáláshoz.
        // Inicializálja a _context mezőt a megadott ApplicationDbContext példánnyal.
        public LiveResultsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Nyilvános tulajdonság a keresési eredmények dokumentumlistájának tárolására.
        // Üres listaként inicializálva; ezt a lekérdezés eredményei töltik fel, és a Razor oldal nézet használja.
        public List<Document> Documents { get; set; } = new();

        // Aszinkron kezelőmetódus HTTP GET kérésekhez.
        // Ez a metódus akkor fut le, amikor az oldal egy 'searchTerm' lekérdezési paraméterrel töltődik be (pl. ?searchTerm=keresés).
        // Végrehajtja a keresést és feltölti a Documents tulajdonságot.
        public async Task OnGetAsync(string searchTerm)
        {
            // Ellenőrzi, hogy a keresési kifejezés null, üres vagy csak whitespace karaktereket tartalmaz-e.
            // Ha igen, a Documents-t üres listára állítja, és korán kilép az adatbázis-lekérdezések elkerülése érdekében.
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Documents = new List<Document>();
                return;
            }

            // A keresési kifejezést különálló szavakra (kifejezésekre) bontja.
            // Szóközt használ elválasztóként, és eltávolítja az üres bejegyzéseket (pl. több szóköz esetén).
            // Ez lehetővé teszi több szavas kereséseket, ahol bármelyik kifejezés illeszkedhet.
            var terms = searchTerm.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            // Lekérdezi az adatbázist az illeszkedő dokumentumokért.
            // - _context.Documents: Hozzáfér a DbSet<Document> halmazhoz a dokumentumok lekérdezéséhez.
            // - .Include(d => d.DocumentMetadata): Előre betölti a kapcsolódó metaadatokat, hogy elkerülje a lusta betöltés problémáit.
            // - .Where(...): Szűri a dokumentumokat a keresési kifejezések alapján.
            //   - Minden dokumentumra ellenőrzi, hogy BÁRMELY kifejezés illeszkedik-e:
            //     - A FileName tartalmazza a kifejezést (részsztring illeszkedés).
            //     - VAGY bármely DocumentMetadata elem Key vagy Value mezője tartalmazza a kifejezést.
            // - .ToListAsync(): Aszinkron módon végrehajtja a lekérdezést, és az eredményeket List<Document> típusként adja vissza.
            Documents = await _context.Documents
                .Include(d => d.DocumentMetadata)
                .Where(d =>
                    terms.Any(term =>
                        d.FileName.Contains(term) ||
                        d.DocumentMetadata.Any(m =>
                            m.Key.Contains(term) ||
                            m.Value.Contains(term))))
                .ToListAsync();
        }
    }
}