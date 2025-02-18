using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RssToTelegram.Data;
using System.ServiceModel.Syndication;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

var telegramBotToken = builder.Configuration["TelegramBotToken"];

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

CancellationTokenSource cts = new CancellationTokenSource();

//List<Flow> flows = [];

//app.MapGet("/flows",() => flows);

//app.MapPost("/addFlow", (Flow flow) =>{
    //flows.Add(flow);
    //return Results.Ok(flow);
//});

var bot = new TelegramBotClient(telegramBotToken!);

DateTimeOffset lastPostDate = new();

async Task checkRSS(){

    try
    {
        while (!cts.IsCancellationRequested)
        {
            string rssUrl = "https://feed.alternativeto.net/news/all/";
    try
        {
            XmlReader rssReader = XmlReader.Create(rssUrl);
SyndicationFeed rssFeed = SyndicationFeed.Load(rssReader);

var firstItem = rssFeed.Items.FirstOrDefault();

if (firstItem != null && firstItem.PublishDate != lastPostDate) {

           
            
            var message = $"{firstItem.Title.Text}\n{firstItem.Summary.Text}\n{firstItem.Links[0].Uri}";
            Console.WriteLine(message);
        
            lastPostDate = firstItem.PublishDate;
            await bot.SendMessage("-1002342956495",message,parseMode: ParseMode.Markdown);
        }


        }
        catch (Exception ex)
        {
            Console.WriteLine("Une erreur est survenue : " + ex.Message);
        }
            
            
        await Task.Delay(TimeSpan.FromMinutes(15), cts.Token); // Simule du travail de manière asynchrone
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Travail annulé.");
    }
}

app.MapGet("/launch", () => {
    Task.Run(async() => await checkRSS());
    return Results.Ok("Tache Lancée");
});

app.MapGet("/stop",() => {
  cts.Cancel();
  return Results.Ok("Tache Arretée");
});

await app.RunAsync();
