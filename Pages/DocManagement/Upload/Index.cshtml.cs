using Microsoft.AspNetCore.Mvc;
     using Microsoft.AspNetCore.Mvc.RazorPages;
     using Cloud9_2.Models;
     using Cloud9_2.Data;
     using Cloud9_2.Services;
     using System.IO;
     using System.Threading.Tasks;
     using System.Linq;
     using Microsoft.EntityFrameworkCore;
     using Microsoft.AspNetCore.Identity;
     using System.Globalization;

     namespace Cloud9_2.Pages.DocManagement
     {
         public class IndexModel : PageModel
         {
             private readonly ApplicationDbContext _context;
            //  private readonly OpenSearchService _openSearchService;
             private readonly IWebHostEnvironment _environment;
             private readonly MetadataExtractor _extractor;
             private readonly UserManager<ApplicationUser> _userManager;

             public IndexModel(ApplicationDbContext context,  IWebHostEnvironment environment, MetadataExtractor extractor, UserManager<ApplicationUser> userManager)
             {
                 _context = context;
                //  _openSearchService = openSearchService;
                 _environment = environment;
                 _extractor = extractor; // No IConfiguration needed
                 _userManager = userManager;
             }

             [BindProperty]
             public IFormFile FormFile { get; set; }
             public string ErrorMessage { get; set; }

             public async Task<IActionResult> OnPostAsync()
             {
                 if (FormFile == null || FormFile.Length == 0)
                 {
                     ErrorMessage = "Kérjük, válasszon ki egy fájlt.";
                     return Page();
                 }

                 if (FormFile.ContentType != "application/pdf")
                 {
                     ErrorMessage = "Csak PDF fájlok támogatottak.";
                     return Page();
                 }

                 var filePath = Path.Combine(_environment.WebRootPath, "Uploads", FormFile.FileName);
                 if (System.IO.File.Exists(filePath))
                 {
                     ErrorMessage = "Ez a fájl már létezik. Kérjük, válasszon másik nevet vagy törölje a meglévőt.";
                     return Page();
                 }

                 Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                 using (var stream = new FileStream(filePath, FileMode.Create))
                 {
                     await FormFile.CopyToAsync(stream);
                 }

                 var (documentType, metadata) = await _extractor.ExtractMetadata(filePath);

                 var docType = await _context.DocumentTypes
                     .FirstOrDefaultAsync(dt => dt.Name == documentType);
                 if (docType == null)
                 {
                     docType = new DocumentType { Name = documentType };
                     _context.DocumentTypes.Add(docType);
                     await _context.SaveChangesAsync();
                 }

                 var document = new Document
                 {
                     FileName = FormFile.FileName,
                     FilePath = filePath,
                     DocumentTypeId = docType.DocumentTypeId,
                     UploadDate = DateTime.UtcNow,
                     UploadedBy = _userManager.GetUserName(User) ?? "System",
                     SiteId = null,
                     PartnerId = null,
                     DocumentMetadata = metadata.Any() ? metadata.Select(kvp => new DocumentMetadata
                     {
                         Key = kvp.Key,
                         Value = kvp.Value
                     }).ToList() : new List<DocumentMetadata>(),
                     DocumentLinks = new List<DocumentLink>()
                 };

                 _context.Documents.Add(document);
                 await _context.SaveChangesAsync();
                 Console.WriteLine($"Saved document {document.DocumentId} with type {documentType} and metadata: {string.Join(", ", metadata.Select(m => $"{m.Key}: {m.Value}"))}");

                //  await _openSearchService.IndexDocument(document);

                 return RedirectToPage("Index");
             }
         }
     }