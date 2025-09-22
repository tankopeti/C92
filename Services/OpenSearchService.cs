// using OpenSearch.Client;
// using Microsoft.Extensions.Configuration;
// using Microsoft.EntityFrameworkCore;
// using Cloud9_2.Data;
// using Cloud9_2.Models;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;

// namespace Cloud9_2.Services
// {
//     public class OpenSearchService
//     {
//         private readonly OpenSearchClient _client;
//         private readonly ApplicationDbContext _context;
//         private bool _isInitialized;

//         public class MetadataItem
//         {
//             public string Key { get; set; } = string.Empty;
//             public string Value { get; set; } = string.Empty;
//         }

//         public class LinkItem
//         {
//             public string ModuleID { get; set; } = string.Empty;
//             public int RecordID { get; set; }
//         }

//         public class IndexedDocument
//         {
//             public int Id { get; set; }
//             public string FileName { get; set; } = string.Empty;
//             public string FilePath { get; set; } = string.Empty;
//             public DateTime? UploadDate { get; set; }
//             public string UploadedBy { get; set; } = string.Empty;
//             public List<MetadataItem> Metadata { get; set; } = new();
//             public List<LinkItem> Links { get; set; } = new();
//         }

//         public class SearchResult
//         {
//             public int Id { get; set; }
//             public string FileName { get; set; } = string.Empty;
//             public string FilePath { get; set; } = string.Empty;
//             public DateTime? UploadDate { get; set; }
//             public string UploadedBy { get; set; } = string.Empty;
//             public List<MetadataItem> Metadata { get; set; } = new();
//         }

//         public OpenSearchService(ApplicationDbContext context, IConfiguration configuration)
//         {
//             var url = configuration["OpenSearch:Url"] ?? "http://localhost:9200";
//             var settings = new ConnectionSettings(new Uri(url))
//                 .DefaultIndex("documentsindex");

//             _client = new OpenSearchClient(settings);
//             _context = context;
//         }

//         public async Task<(bool IsConnected, string? ErrorMessage)> TestConnectionAsync()
//         {
//             try
//             {
//                 var ping = await _client.PingAsync();
//                 return ping.IsValid
//                     ? (true, null)
//                     : (false, ping.OriginalException?.Message ?? "Unknown error");
//             }
//             catch (Exception ex)
//             {
//                 return (false, ex.Message);
//             }
//         }

//         public async Task InitializeAsync()
//         {
//             if (_isInitialized) return;

//             var (isConnected, errorMessage) = await TestConnectionAsync();
//             if (!isConnected) throw new Exception(errorMessage);

//             await CreateIndexWithMappingAsync();
//             _isInitialized = true;
//         }

//         private async Task CreateIndexWithMappingAsync()
//         {
//             var exists = await _client.Indices.ExistsAsync("documentsindex");
//             if (!exists.Exists)
//             {
//                 var create = await _client.Indices.CreateAsync("documentsindex", c => c
//                     .Map<IndexedDocument>(m => m
//                         .Properties(p => p
//                             .Text(t => t.Name(n => n.FileName).Fields(f => f.Keyword(k => k.Name("keyword"))))
//                             .Text(t => t.Name(n => n.FilePath).Fields(f => f.Keyword(k => k.Name("keyword"))))
//                             .Keyword(k => k.Name(n => n.UploadedBy))
//                             .Date(d => d.Name(n => n.UploadDate))
//                             .Nested<MetadataItem>(n => n
//                                 .Name("Metadata")
//                                 .Properties(np => np
//                                     .Keyword(k => k.Name(m => m.Key))
//                                     .Text(t => t.Name(m => m.Value))
//                                 )
//                             )
//                             .Nested<LinkItem>(n => n
//                                 .Name("Links")
//                                 .Properties(np => np
//                                     .Keyword(k => k.Name(l => l.ModuleID))
//                                     .Number(nu => nu.Name(l => l.RecordID).Type(NumberType.Integer))
//                                 )
//                             )
//                         )
//                     )
//                 );

//                 if (!create.IsValid)
//                     throw new Exception(create.OriginalException?.Message ?? "Index creation failed");
//             }
//         }

//         public async Task IndexAllData()
//         {
//             await InitializeAsync();
//             var documents = await _context.Documents
//                 .Include(d => d.DocumentMetadata)
//                 .Include(d => d.DocumentLinks)
//                 .ToListAsync();

//             foreach (var doc in documents)
//             {
//                 await IndexDocument(doc);
//             }
//         }

//         public async Task IndexDocument(Document doc)
//         {
//             await InitializeAsync();

//             var metadata = doc.DocumentMetadata?.Select(m => new MetadataItem { Key = m.Key, Value = m.Value }).ToList() ?? new();
//             var links = doc.DocumentLinks?.Select(l => new LinkItem { ModuleID = l.ModuleID, RecordID = l.RecordID }).ToList() ?? new();

//             var document = new IndexedDocument
//             {
//                 Id = doc.DocumentId,
//                 FileName = doc.FileName ?? string.Empty,
//                 FilePath = doc.FilePath ?? string.Empty,
//                 UploadDate = doc.UploadDate,
//                 UploadedBy = doc.UploadedBy ?? string.Empty,
//                 Metadata = metadata,
//                 Links = links
//             };

//             var index = await _client.IndexAsync(document, i => i.Index("documentsindex").Id(doc.DocumentId.ToString()));
//             if (!index.IsValid)
//                 throw new Exception($"Failed to index document {doc.DocumentId}: {index.OriginalException?.Message}");
//         }

//         public async Task<List<SearchResult>> SearchDocumentsAsync(string query)
//         {
//             await InitializeAsync();

//             QueryContainer queryContainer;
//             if (query.Contains(":"))
//             {
//                 var parts = query.Split(':', 2);
//                 var key = parts[0].Trim();
//                 var value = parts[1].Trim();

//                 queryContainer = new QueryContainerDescriptor<IndexedDocument>()
//                     .Nested(n => n
//                         .Path(d => d.Metadata)
//                         .Query(q => q
//                             .Bool(b => b
//                                 .Filter(
//                                     f => f.Term(t => t.Field("Metadata.Key").Value(key)),
//                                     f => f.Wildcard(w => w.Field("Metadata.Value").Value($"*{value}*"))
//                                 )
//                             )
//                         )
//                     );
//             }
//             else
//             {
//                 queryContainer = new QueryContainerDescriptor<IndexedDocument>()
//                     .Bool(b => b
//                         .Should(
//                             s => s.Wildcard(w => w.Field(f => f.FileName).Value($"*{query}*")),
//                             s => s.Wildcard(w => w.Field(f => f.FilePath).Value($"*{query}*")),
//                             s => s.Wildcard(w => w.Field(f => f.UploadedBy).Value($"*{query}*")),
//                             s => s.Nested(n => n
//                                 .Path(d => d.Metadata)
//                                 .Query(q => q.Wildcard(w => w.Field("Metadata.Value").Value($"*{query}*")))
//                             )
//                         )
//                         .MinimumShouldMatch(1)
//                     );
//             }

//             var response = await _client.SearchAsync<IndexedDocument>(s => s
//                 .Index("documentsindex")
//                 .Query(q => queryContainer)
//             );

//             if (!response.IsValid)
//                 throw new Exception(response.OriginalException?.Message ?? "Search query failed");

//             return response.Hits.Select(hit => new SearchResult
//             {
//                 Id = hit.Source.Id,
//                 FileName = hit.Source.FileName,
//                 FilePath = hit.Source.FilePath,
//                 UploadDate = hit.Source.UploadDate,
//                 UploadedBy = hit.Source.UploadedBy,
//                 Metadata = hit.Source.Metadata
//             }).ToList();
//         }
//     }
// }
