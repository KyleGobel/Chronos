using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Chronos;
using Chronos.Dapper.Chronos.Dapper;
using Chronos.Interfaces;
using ServiceStack;
using Xunit;

namespace Test
{
    public class TempEngine : ITemplateEngine
    {
        public string RenderToString(string template, object model)
        {
            return Nustache.Core.Render.StringToString(template, model);
        }



    }

    public class MeMe
    {
        public DateTime Date { get; set; }
        public string AdId { get; set; }
        public string AdName { get; set; }
    }
}