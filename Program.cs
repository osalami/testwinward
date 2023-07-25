using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using WindwardRestApi.src.Api;
using WindwardRestApi.src.Model;
namespace TestWinward
{
    class Program
    {
        static async Task Main(string[] args)
        {

            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string FilePath = System.IO.Path.GetDirectoryName(strExeFilePath);

            var client = new WindwardClient(new Uri("https://aprsye.dev.afreximbank.net"));
            /* Get the version info */
            var version = await client.GetVersion();
            Console.WriteLine(version);
            string templatePath = $"{FilePath}/Template/Account_Statement.docx";
            // Create the template object, based on the file extension
            Template.OutputFormatEnum formatOutput = Template.OutputFormatEnum.Pdf;
            Template.FormatEnum formatTemplate = Path.GetExtension(templatePath).Substring(1).GetEnumFromValue<Template.FormatEnum>();
            Template template = new Template(formatOutput, File.ReadAllBytes(templatePath), formatTemplate);
            var JSONDatasource = new JsonDataSource("JSON", File.ReadAllBytes($"{FilePath}/Template/pdf-stmt-sample-data.json"));
            template.Datasources.Add(JSONDatasource);
            Document postDocument = await client.PostDocument(template);
            string guid = postDocument.Guid;
            HttpStatusCode status = await client.GetDocumentStatus(guid);
            while (status == HttpStatusCode.Created || status == HttpStatusCode.Accepted)
            {
                await Task.Delay(100);
                status = await client.GetDocumentStatus(guid);
            }
            Document document = await client.GetDocument(guid);
            // save
            if (document.Data != null)
            {
                //string fileDirectory = Path.GetDirectoryName(Path.GetFullPath(templatePath));
                string extension = formatOutput.ToString().ToLower();
                string fileName =   $"{FilePath}/Output/" + document.Guid + "." + extension;
                File.WriteAllBytes(fileName, document.Data);
                Console.Out.WriteLine("Output page written to " + fileName);
            }
            else
            {
                {
                    string prefix = Path.GetFileNameWithoutExtension(templatePath);
                    string directory = Path.GetDirectoryName(Path.GetFullPath(templatePath));
                    string extension = Path.GetExtension(templatePath);
                    for (int fileNumber = 0; fileNumber < document.Pages.Length; fileNumber++)
                    {
                        string filename = Path.Combine(directory, prefix + "_" + fileNumber + extension);
                        filename = Path.GetFullPath(filename);
                        File.WriteAllBytes(filename, document.Pages[fileNumber]);
                        Console.Out.WriteLine("  document page written to " + templatePath);
                    }
                }
            }
            // delete it off the server
            await client.DeleteDocument(guid);
        }
    }
}