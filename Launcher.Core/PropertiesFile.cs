using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Launcher.Core
{
    //File format:
    //# comment
    //key=value

    public class PropertiesFile
    {
        private Dictionary<string, string> dict = new Dictionary<string, string>();

        public PropertiesFile(Stream fileStream)
        {
            using (StreamReader sr = new StreamReader(fileStream))
            {
                var regex = new Regex(@"(.+?)=(.*)");

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    int i = line.IndexOf('#');
                    if (i >= 0) line = line.Substring(0, i);
                    line = line.TrimStart();
                    if (line.Length != 0)
                    {
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            dict[match.Groups[1].Value] = match.Groups[2].Value;
                        }
                    }
                }
            }
        }

        public string this[string key]
        {
            get
            {
                if (dict.ContainsKey(key))
                    return dict[key];
                else
                    return null;
            }
        }
    }
}
