namespace Chronos.Interfaces
{
    public interface ITemplateEngine
    {
        string RenderToString(string template, object model);
    }
}