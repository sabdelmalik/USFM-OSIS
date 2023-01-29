using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SM.Bible.Formats
{
    /// <summary>
    /// This code is based on the Python code by Chris Little https://github.com/chrislit/usfm2osis
    /// which is available under GNU General Public License v3.0.
    /// </summary>
    internal class UsfmTags
    {
        public UsfmTags(string[] args)
        {
            if (args.Contains("-h") || args.Contains("--help") || args.Length < 1)
            {
                print_usage();
                return;
            }

            foreach (string doc in args)
            { 
                if(doc.Contains('*') || doc.Contains('?'))
                {
                    var folder = Path.GetDirectoryName(doc);
                    if (folder != null && Directory.Exists(folder)) { 
                        // Get all files matching filename with wildcard.
                        string[] files = Directory.GetFiles(folder, Path.GetFileName(doc));
                        if (files == null || files.Length == 0)
                            return;
                        foreach (string file in files) {
                            check_tags(file);
                        }
                    }
                }
                else
                {
                    check_tags(doc);
                }
            }
        }


        private void check_tags(string doc)
        {
            List<string> known_set = new List<string>();
            List<string> unknown_set = new List<string>();

            if (File.Exists(doc))
            {
                Console.WriteLine("Result for file: " + doc);
                using (StreamReader sr = new StreamReader(doc, Encoding.UTF8))
                {
                    string text = sr.ReadToEnd();
                    Regex rx = new Regex(@"(\\[a-zA-Z0-9]+\b\*?)");
                    MatchCollection matches = rx.Matches(text);
                    Console.WriteLine("{0} matches found", matches.Count);
                    foreach (Match tag in matches)
                    {
                        if (Constants.SIMPLE_TAGS.Contains(tag.Value))
                        {
                            if (!known_set.Contains(tag.Value)) known_set.Add(tag.Value);
                        }
                        else
                        {
                            string t = Regex.Replace(tag.Value, @"[\d-]", string.Empty);
                            if (Constants.DIGIT_TAGS.Contains(t))
                            {
                                if (!known_set.Contains(tag.Value)) known_set.Add(tag.Value);
                            }
                            else
                            {
                                unknown_set.Add(tag.Value);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Known USFM Tags: " + string.Join(",", known_set.OrderBy(q => q).ToList()));
            Console.WriteLine("Unrecognized USFM Tags: " + string.Join(",", unknown_set.OrderBy(q => q).ToList()));
            Console.WriteLine("");

        }

        private void print_usage()
        {
            Console.WriteLine("usfmtags <USFM filenames|wildcard>");
            Console.WriteLine("");
            Console.WriteLine(
                " This utility will scan USFM files and Console.WriteLine two lists of all "
                + "unique tags in\nthem."
            );
            Console.WriteLine(
                " The first list identifies all valid tags, identified in the USFM "
                + Constants.USFM_VERSION
                + " spec."
            ); ;
            Console.WriteLine(" The second list identifies tags unknown to that spec.");
    }




    }
}
