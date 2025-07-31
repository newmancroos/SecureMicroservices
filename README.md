# SecureMicroservices


## Adding Identituservice UI
* Refer this Link : https://github.com/DuendeArchive/IdentityServer.Quickstart.UI
  1. Run the following command in Identityserver project terminal
     <pre> iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/DuendeSoftware/IdentityServer.Quickstart.UI/main/getmain.ps1'))</pre>
     This will add some new folders with code with html pages
  2. Add <pre>builder.Services.AddControllersWithViews();</pre>  in to the program.cs file
  3. Add <pre>app.UseStaticFiles();</pre> in program.cs
  4. add
     <pre>
      app.UseEndpoints(endpoints =>
      {
          endpoints.MapDefaultControllerRoute();
      });
     </pre>
     
     
