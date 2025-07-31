# SecureMicroservices


## Adding Identituservice UI
* Refer this Link : https://github.com/DuendeArchive/IdentityServer.Quickstart.UI
  1. Run the following command in Identityserver project terminal
     <pre> iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/DuendeSoftware/IdentityServer.Quickstart.UI/main/getmain.ps1'))</pre>
     This will add some new folders with code with html pages
  2. Duende  Identity server uses Razer pages instead of mvc template
     <pre>
      builder.Services.AddRazorPages();
      builder.Services.AddAuthentication();

       app.UseStaticFiles();
      app.UseHttpsRedirection();
      app.UseRouting();
      app.UseIdentityServer();
      app.UseAuthorization();
      app.MapRazorPages()
          .RequireAuthorization();
     </pre>  in to the program.cs file
  4. Add <pre>app.UseStaticFiles();</pre> in program.cs
  5. add
     <pre>
      app.UseRouting();
      app.UseEndpoints(endpoints =>
      {
          endpoints.MapDefaultControllerRoute();
      });
     </pre>
     
     
