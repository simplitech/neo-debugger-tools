using System.Collections.Generic;
using System.IO;
using System.Web;
using Humanizer;
using Neo.Debugger.Core.Data;

namespace Neo.WebDebugger.Logic
{
    public static class ContractsHelper
    {
        public static Dictionary<string,string> GetContractList(SourceLanguage language)
        {
            Dictionary<string, string> contracts = new Dictionary<string, string>();
            string templatePath = Path.Combine(HttpContext.Current.Server.MapPath("~"), "Contracts", language.ToString());

            if (!Directory.Exists(templatePath))
                throw new DirectoryNotFoundException(templatePath);

            var files = Directory.EnumerateFiles(templatePath);
            foreach (var file in files)
                contracts.Add(file, Path.GetFileNameWithoutExtension(file).Humanize());

            return contracts;
        }   

        public static string GetContractContent(string contractFileName)
        {
            SourceLanguage language = SourceLanguage.Other;

            //Check the filename for the language
            if (contractFileName.ToLower().EndsWith(".cs"))
                language = SourceLanguage.CSharp;
            else if (contractFileName.ToLower().EndsWith(".py"))
                language = SourceLanguage.Python;

            string templateFile = Path.Combine(HttpContext.Current.Server.MapPath("~"), "Contracts", language.ToString(), contractFileName);

            if (!File.Exists(templateFile))
                throw new FileNotFoundException(templateFile);

            return File.ReadAllText(templateFile);
        }
    }
}