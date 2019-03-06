using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

#pragma warning disable 1591

namespace MarginTrading.Backend.Email
{
    public class MustacheTemplateGenerator : ITemplateGenerator
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly string _templatesFolder;

        public MustacheTemplateGenerator(IHostingEnvironment hostingEnvironment, string templatesFolder)
        {
            _hostingEnvironment = hostingEnvironment;
            _templatesFolder = templatesFolder;
        }

        public string Generate<T>(string templateName, T model)
        {
            var templatesFolder = Path.Combine(_hostingEnvironment.ContentRootPath, _templatesFolder);

            var path = Path.Combine(templatesFolder, templateName + ".mustache");

            try
            {
                return Nustache.Core.Render.FileToString(path, model);
            }
            catch (InvalidCastException)
            {
                Console.WriteLine($"Incorrect model was passed for template: {path}");
                throw;
            }
        }
    }
}
