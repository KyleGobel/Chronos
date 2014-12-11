using Chronos.Interfaces;

namespace Test
{
    public class TempEngine : ITemplateEngine
    {
        public string RenderToString(string template, object model)
        {
            return Nustache.Core.Render.StringToString(template, model);
        }
    }
}