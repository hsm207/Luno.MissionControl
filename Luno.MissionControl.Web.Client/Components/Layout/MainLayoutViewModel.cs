namespace Luno.MissionControl.Web.Client.Components.Layout;

public class MainLayoutViewModel
{
    public string AppTitle { get; set; } = "MISSION CONTROL";
    public string StatusText { get; set; } = "DEVELOPMENT";
    public string StatusClass { get; set; } = "gold-glow";
    public bool IsNavigationCollapsed { get; set; } = false;
    
    public List<NavigationItem> NavItems { get; } = new()
    {
        new NavigationItem { Text = "Dashboard", Href = "/", Icon = "Board" },
        new NavigationItem { Text = "Combo Orchestrator", Href = "/combo", Icon = "Cart" }
    };
}

public class NavigationItem
{
    public string Text { get; set; } = "";
    public string Href { get; set; } = "";
    public string Icon { get; set; } = "";
}
