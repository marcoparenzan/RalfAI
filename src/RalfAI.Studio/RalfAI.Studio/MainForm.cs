using Microsoft.AspNetCore.Components.WebView.WindowsForms;

namespace RalfAI.Studio;

public sealed class MainForm : Form
{
    public MainForm(IServiceProvider services)
    {
        Text = $"RalfAI Studio v{typeof(MainForm).Assembly.GetName().Version?.ToString(3)}";
        Width = 1500;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        var webView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = services
        };
        webView.RootComponents.Add<App>("#app");
        Controls.Add(webView);
    }
}
