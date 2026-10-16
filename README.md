# 📄 Document Processing System — .NET Framework 4.8 / Web Forms

An ASP.NET Web Forms application that summarizes uploaded documents with Claude Sonnet 5 on
Amazon Bedrock, storing results in SQL Server.

This is the **legacy counterpart** of the `net10-sqlserver` branch, which holds the
same application as a .NET 10 Blazor Server app. Same features, same database, same visual
design — written the way it would have been written in 2015. It exists to be a realistic
"before" state for a .NET modernization exercise.

## 🌟 Features

- **🤖 AI Summaries**: Amazon Bedrock Converse API with Claude Sonnet 5
- **📄 Text Extraction**: PDF text extraction via PdfPig, plus plain-text and log files
- **📤 Upload**: Drag files onto the dropzone or browse, with size and file-type validation
- **🗂️ Document List**: Newest first, with upload timestamp, status, and summary preview
- **💾 SQL Server**: Entity Framework 6 code-first, with a local instance in Docker
- **🔐 Credentials**: Optional AWS Secrets Manager lookup for RDS credentials
- **🧭 Provider Indicator**: Header pill reports the database engine actually in use

## 🏗️ Architecture

One ASP.NET Web Forms project. A master page supplies the shell, two user controls cover
upload and listing, and a small pipeline handles storage, extraction, and summarization.

```
src/DocumentProcessor.WebForms/
├── App_Data/
│   ├── Schema.sql            # Documents table, created on first run
│   └── uploads/              # date-partitioned upload storage (not served by IIS)
├── Configuration/
│   └── AppSettings.cs        # typed reads over ConfigurationManager
├── Content/                  # Site.css, Bootstrap
├── Controls/
│   ├── DocumentList.ascx     # Repeater, status badges, view/delete commands
│   └── DocumentUploader.ascx # FileUpload, validation, notices
├── Data/
│   ├── DatabaseConnectionResolver.cs
│   ├── DatabaseInitializer.cs
│   ├── DatabaseInfo.cs
│   └── DocumentDbContext.cs  # EF6
├── Models/                   # Document, DocumentStatus, Notification
├── Scripts/                  # Bootstrap bundle
├── Services/                 # storage, text extraction, summarization, pipeline
├── Default.aspx              # the one page: grid, UpdatePanel, modals
├── ErrorPage.aspx
├── Global.asax               # startup: TLS, connection resolve, schema, tracing
├── Site.Master               # app bar, database pill, footer
└── Web.config
```

Upload flow:

```
Browser → Default.aspx postback → IDocumentStorage (disk)
                                → DocumentDbContext (row, status Pending)
        → DocumentPipeline → DocumentTextExtractor (PdfPig)
                           → IDocumentSummarizer (Bedrock Converse)
                           → DocumentDbContext (summary, status Processed)
```

Uploading is a full postback; refresh, view-summary, and delete go through an `UpdatePanel`.
A file input cannot be posted through an asynchronous postback, which is why the uploader
sits outside the panel.

## 🚀 Getting Started

### Prerequisites

- Visual Studio 2022 or later with the **ASP.NET and web development** workload, or
  MSBuild plus the **.NET Framework 4.8 targeting pack**
- IIS Express (installed with the workload above)
- Docker (for local SQL Server) or an existing SQL Server instance
- An AWS account with Bedrock access to Claude Sonnet 5 in your chosen region
- AWS credentials available to the default SDK chain

### Installation

1. **Clone the repository and check out this branch**
   ```bash
   git clone https://github.com/aws-samples/sample-document-processing-system.git
   cd sample-document-processing-system
   git checkout legacy-net48
   ```

2. **Configure AWS credentials**
   ```bash
   aws configure
   ```
   Or set `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and `AWS_DEFAULT_REGION`.

   Confirm the model is reachable:
   ```bash
   aws bedrock-runtime converse \
     --region us-east-1 \
     --model-id global.anthropic.claude-sonnet-5 \
     --messages '[{"role":"user","content":[{"text":"Reply with: OK"}]}]' \
     --inference-config '{"maxTokens":20}'
   ```

3. **Start SQL Server**
   ```bash
   docker compose up -d
   ```

   Runs `mcr.microsoft.com/mssql/server:2022-latest` on port 1433 with the `sa` password
   `LocalDev!Passw0rd`, matching the connection string in `Web.config`. Override it by
   setting `MSSQL_SA_PASSWORD` before `docker compose up` and updating the connection
   string to match. Data persists in the `sqlserver-data` volume.

4. **Restore packages and build**

   This project uses `packages.config`, so restore runs through MSBuild rather than
   `dotnet restore`:
   ```bash
   msbuild DocumentProcessor.sln -t:Restore -p:RestorePackagesConfig=true
   msbuild DocumentProcessor.sln -t:Build -p:Configuration=Debug
   ```

   In Visual Studio, opening the solution and pressing F5 does both.

5. **Run the application**

   From Visual Studio, press F5. To run it without the IDE:
   ```bash
   "C:\Program Files\IIS Express\iisexpress.exe" \
     /path:"%CD%\src\DocumentProcessor.WebForms" /port:44821
   ```

   The `DPS` database and `Documents` table are created on first request.

6. **Open the app**

   Navigate to <http://localhost:44821/>.

## ⚙️ Configuration

Everything lives in `Web.config`. `Configuration/AppSettings.cs` reads it; there is no
validation at startup, so a malformed value surfaces as an exception the first time the
setting is touched.

| Setting | Default | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `localhost,1433` / `DPS` | SQL Server |
| `Database.UseSecretsManager` | `false` | When true, credentials come from Secrets Manager |
| `Database.SecretDescriptionPrefix` | `Password for RDS MSSQL used for MAM319.` | Secret is matched on its description |
| `Bedrock.Region` | `us-east-1` | |
| `Bedrock.SummarizationModelId` | `global.anthropic.claude-sonnet-5` | |
| `Bedrock.MaxTokens` | `2000` | |
| `Bedrock.MaxInputCharacters` | `10000` | Characters of extracted text sent to the model |
| `Bedrock.MaxPdfPages` | `5` | |
| `Storage.RootPath` | `~/App_Data/uploads` | Virtual path, resolved with `MapPath` |
| `Storage.MaxFileSizeMegabytes` | `50` | |
| `Storage.MaxFilesPerUpload` | `10` | |
| `Storage.AllowedExtensions` | `.pdf,.txt,.log` | Comma separated |

`httpRuntime maxRequestLength` and `requestFiltering maxAllowedContentLength` must be large
enough for `MaxFilesPerUpload × MaxFileSizeMegabytes`; they are set to 500 MB to match the
defaults above. Change all of them together.

### Sharing a database with the .NET 10 branch

`App_Data/Schema.sql` creates the `Documents` table with the same column types and lengths
that EF Core generates for the same entity, so both branches can point at one `DPS`
database and see each other's rows. EF6's initializer is disabled
(`Database.SetInitializer<DocumentDbContext>(null)`) so it never tries to own the schema or
write a `__MigrationHistory` table.

### Logging

Errors go to `System.Diagnostics.Trace`. `Global.asax` attaches a listener writing to
`App_Data/trace.log`, which is the quickest place to look when something fails. Note that
the listener holds the file open for the lifetime of the application, so you cannot delete
it while the app is running. A production app would use log4net or ELMAH instead.

## 🧭 Notes on the legacy design

These are deliberate, and each one is a talking point for a modernization pass.

- **Blocking I/O on request threads.** The .NET Framework build of the AWS SDK exposes real
  synchronous operations, so `Converse` is called directly rather than awaited. Bedrock can
  take tens of seconds, and each upload holds an ASP.NET thread for the whole call. That is
  why `executionTimeout` is 600 seconds.
- **No dependency injection.** Services are constructed at the point of use. Interfaces
  survive on the seams that matter (`IDocumentStorage`, `IDocumentSummarizer`), and
  `DocumentPipeline` has a constructor that accepts them, but nothing wires a container.
- **Soft delete is manual.** EF6 has no global query filter, so every query repeats
  `!d.IsDeleted`. Forgetting it in one place silently resurrects deleted rows.
- **Whole uploads are buffered.** ASP.NET reads the entire multipart body before the handler
  runs, so a ten-file batch is in memory or on disk up front. The Blazor build streams each
  file instead.
- **Configuration is stringly typed.** `ConfigurationManager` plus `int.Parse`, with no
  options binding and no startup validation.
- **State rides in ViewState.** The delete confirmation remembers its target through
  `ViewState`, and the document list survives postbacks as serialized control state rather
  than being requeried.
- **The selected-file list needs script.** The server has no idea what is in a file input
  until the form posts, so a small inline script renders the pending list. Removing one file
  from a selection is not possible the way it is in the Blazor build, so the control offers
  Clear instead.
- **Upload progress is a client-side illusion.** A full postback gives the server no way to
  report progress, so script swaps the button into a spinner and prints a waiting notice
  before the form goes. The Blazor build shows a per-file result as each one finishes,
  because its render loop can yield mid-upload; here every notice appears at once when the
  response lands. Note the upload control is a `LinkButton`, not a `Button`: disabling a
  submit button before the post strips its name from the request and the server-side Click
  handler never runs, whereas `__doPostBack` carries the target in `__EVENTTARGET`.

## ✅ What has been verified

Built with MSBuild against .NET Framework 4.8 with no warnings, precompiled with
`aspnet_compiler` to check every `.aspx`/`.ascx` and data-binding expression, and exercised
end to end against the Dockerized SQL Server and live Bedrock: upload → disk → database →
text extraction → summary → list ordering → view-summary modal → delete confirmation →
soft delete → refresh.

No screenshots are checked in on this branch.
