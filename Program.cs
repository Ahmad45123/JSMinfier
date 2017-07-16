using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsBuilder
{
    class Program
    {
        private static string _nodeExec;
        private static string _inDir;
        private static string _outDir;

        static void Main(string[] args)
        {
            _nodeExec = args[0];
            _inDir = args[1];
            _outDir = args[2];

            if (Directory.Exists(_outDir))
            {
                Console.WriteLine("Deleting old results folder.");
                Directory.Delete(_outDir, true);
            }

            Console.WriteLine("Starting JS build process.");
            foreach (string f in Directory.GetFiles(_inDir, "*.*", SearchOption.AllDirectories)
                .Where(x => x.EndsWith("js") || x.EndsWith("html") || x.EndsWith("png") || x.EndsWith("css") ||
                            x.EndsWith("meta.xml") || x.EndsWith("tff") || x.EndsWith("woff") || x.EndsWith("woff2") ||
                            x.EndsWith("mp3") || x.EndsWith("svg") || x.EndsWith("eot") || x.EndsWith("json")))
            {
                Console.WriteLine("Processing: " + f);

                if (f.EndsWith(".js"))
                {
                    Process(f);
                }
                else if (f.EndsWith(".html"))
                {
                    var htmlCode = File.ReadAllText(f);

                    //Detect all JS.
                    foreach (Match match in Regex.Matches(htmlCode, @"<script>(?<code>.*)<\/script>", RegexOptions.Singleline))
                    {
                        File.WriteAllText(Path.Combine(_inDir, "temp.js"), match.Groups["code"].Value); //Write code to file.
                        Process(Path.Combine(_inDir, "temp.js")); //Obfuscate
                        var result = File.ReadAllText(Path.Combine(_outDir, "temp.js")); //Get results.
                        File.Delete(Path.Combine(_outDir, "temp.js")); //Delete file
                        File.Delete(Path.Combine(_inDir, "temp.js")); //Delete file
                        htmlCode = htmlCode.Remove(match.Groups["code"].Index, match.Groups["code"].Length); //Delete old code.
                        htmlCode = htmlCode.Insert(match.Groups["code"].Index, result); //Inject obfuscated code.
                    }

                    //Detect all CSS
                    foreach (Match match in Regex.Matches(htmlCode, @"<style>(?<code>.*)<\/style>",
                        RegexOptions.Singleline))
                    {
                        var result = Regex.Replace(match.Groups["code"].Value, @"\s*([,>+;:}{]{1})\s*", "$1").Replace(";}", "}");
                        htmlCode = htmlCode.Remove(match.Groups["code"].Index, match.Groups["code"].Length); //Delete old code.
                        htmlCode = htmlCode.Insert(match.Groups["code"].Index, result); //Inject obfuscated code.
                    }

                    //Remove HTML whitspace.
                    htmlCode = Regex.Replace(htmlCode, @"(?<=>)\s+(?=<)|(?<=>)\s+(?!=<)|(?!<=>)\s+(?=<)", @"");

                    //Remove repeated whitespace.
                    htmlCode = Regex.Replace(htmlCode, @"\s+", @" ");

                    //Now write file.
                    var relPath = f.Replace(_inDir, "").Remove(0, 1);
                    var path = Path.Combine(_outDir, relPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, htmlCode);
                }
                else if (f.EndsWith(".css"))
                {
                    //Remove whitespace
                    var code = Regex.Replace(File.ReadAllText(f), @"\s*([,>+;:}{]{1})\s*", "$1").Replace(";}", "}");

                    //Now write file.
                    var relPath = f.Replace(_inDir, "").Remove(0, 1);
                    var path = Path.Combine(_outDir, relPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, code);
                }
                else
                {
                    //Prepare path
                    var relPath = f.Replace(_inDir, "").Remove(0, 1);
                    var path = Path.Combine(_outDir, relPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.Copy(f, path, true);
                }
            }
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        static void Process(string fileName)
        {
            //Relativepath
            var relPath = fileName.Replace(_inDir, "").Remove(0, 1);

            //Start the obfuscation process: 
            Process p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    FileName = _nodeExec,
                    WorkingDirectory = _inDir
                }
            };
            //Path to node installed folder****
            string argument =
                $@"..\node_modules\javascript-obfuscator\bin\javascript-obfuscator.js {
                        fileName
                    } --compact true --selfDefending true -o {Path.Combine(_outDir, relPath)}";

            p.StartInfo.Arguments = argument;
            p.Start();
            p.WaitForExit();
        }
    }
}
