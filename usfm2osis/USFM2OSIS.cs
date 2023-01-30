using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SM.Bible.Formats
{
    /// <summary>
    /// This code is based on the Python code by Chris Little https://github.com/chrislit/usfm2osis
    /// which is available under GNU General Public License v3.0.
    /// </summary>
    public partial class USFM2OSIS
    {
        string usfmVersion = "2.35";  // http://ubs-icap.org/chm/usfm/2.35/index.html
        string osisVersion = "2.1.1";  // http://www.bibletechnologies.net/osisCore.2.1.1.xsd
        string scriptVersion = "0.6.1";

        Dictionary<string, string> osis_to_loc_book = new Dictionary<string, string>();
        Dictionary<string, string> loc_to_osis_book = new Dictionary<string, string>();

        bool relaxed_conformance = false;
        bool debug = false;
        bool verbose = false;
        bool validate_xml = false;
        string lang_code = "und"; // undefined
        string encodingStr = string.Empty;
        Encoding encoding = Encoding.UTF8;
        string osisWork = string.Empty;
        string osis_filename = string.Empty;
        int input_files_index = 1;  // This marks the point in the sys.argv array, after which all values represent USFM files to be converted.
        List<string> work_usfm_doc_list = new List<string>();
        List<string> usfm_doc_list = new List<string>();

        Dictionary<string, string> osisSegment;

        List<string> aliases = new List<string>();
        public USFM2OSIS(string[] args)
        {

            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                aliases.Add(ei.Name.ToLower());
            }


            if (args.Length > 0)
            {
                if (args.Contains("-v"))
                {
                    verbose = true;
                    input_files_index += 1;
                }
                if (args.Contains("-x"))
                {
                    validate_xml = true;
                    input_files_index += 1;
                }
                if (args.Contains("-d"))
                {
                    debug = true;
                    input_files_index += 1;
                    verbose = true;
                }
                if (args.Contains("-t"))
                {
                    /*
                            i = sys.argv.index("-t") + 1
                            if len(sys.argv) < i + 1:
                                print_usage()
                            try:
                                num_processes = max(1, int(sys.argv[i]))
                                input_files_index += 2  # increment 2, reflecting 2 args for -t
                            except ValueError:
                                print_usage()
                     */
                }
                if (args.Contains("-l"))
                {
                    int i = Array.IndexOf(args, "-l") + 1;
                    if (args.Length < i + 1)
                    {
                        print_usage();
                        return;
                    }
                    lang_code = args[i];
                    input_files_index += 2;  // increment 2, reflecting 2 args for -l
                }
                if (args.Contains("-h") || args.Contains("--help") || args.Length < 2)
                {
                    print_usage();
                    return;
                }
                osisWork = args[0];

                if (args.Contains("-o"))
                {
                    int i = Array.IndexOf(args, "-o") + 1;
                    if (args.Length < i + 1)
                    {
                        print_usage();
                        return;
                    }
                    osis_filename = args[i];
                    input_files_index += 2;  // increment 2, reflecting 2 args for -o
                }
                else
                {
                    osis_filename = osisWork + ".osis.xml";
                }
                if (args.Contains("-e"))
                {
                    int i = Array.IndexOf(args, "-e") + 1;
                    if (args.Length < i + 1)
                    {
                        print_usage();
                        return;
                    }
                    encodingStr = args[i];
                    input_files_index += 2;  // increment 2, reflecting 2 args for -e
                }
                if (args.Contains("-r"))
                {
                    relaxed_conformance = true;
                    input_files_index += 1;
                }
                if (args.Contains("-s"))
                {
                    int i = Array.IndexOf(args, "-s") + 1;
                    if (args.Length < i + 1)
                    {
                        print_usage();
                        return;
                    }
                    // sortKey = args[i];
                    input_files_index += 2;  // increment 2, reflecting 2 args for -s
                }
                else
                {
                    // sortKey = key_natural;
                }

                work_usfm_doc_list = args.Skip(input_files_index).ToList();
                work_usfm_doc_list.Sort();

                Console.WriteLine("Converting USFM documents to OSIS...");
                osisSegment = new Dictionary<string, string>();
                foreach (string file in work_usfm_doc_list)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.Contains('*'))
                    {
                        var folder = Path.GetDirectoryName(file);
                        if (folder != null)
                        {
                            string[] docs = Directory.GetFiles(folder, fileName);
                            foreach (string doc in docs)
                            {
                                usfm_doc_list.Add(doc);
                                read_identifiers_from_osis(doc);
                                string osis = ConvertToOSIS(doc);
                                osisSegment[doc] = osis;
                            }
                        }

                    }
                    else
                    {
                        read_identifiers_from_osis(file);
                        string osis = ConvertToOSIS(file);
                        osisSegment[file] = osis;
                        usfm_doc_list.Add(file);
                    }
                }

                Console.WriteLine("Assembling OSIS document");
                string osis_doc = (
                    "<osis xmlns=\"http://www.bibletechnologies.net/2003/OSIS/namespace\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.bibletechnologies.net/2003/OSIS/namespace http://www.bibletechnologies.net/osisCore."
                    + osisVersion
                    + ".xsd\">\n<osisText osisRefWork=\"Bible\" xml:lang=\""
                    + lang_code
                    + "\" osisIDWork=\""
                    + osisWork
                    + "\">\n<header>\n<work osisWork=\""
                    + osisWork
                    + "\"/>\n</header>\n"
                );
                List<string> unhandled_tags = new List<string>();
                foreach (string doc in usfm_doc_list)
                {
                    Regex rx = new Regex(@"(\\[^\s]*)");
                    string text = rx.Match(osisSegment[doc]).Value;
                    if (!string.IsNullOrEmpty(text))
                        unhandled_tags.Add(text);
                    osis_doc += osisSegment[doc];
                }
                osis_doc += "</osisText>\n</osis>\n";

                if (validate_xml)
                {
                    try
                    {

                    }
                    catch { }
                }

                using (StreamWriter sw = new StreamWriter(osis_filename, false, Encoding.UTF8))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sw.Write(osis_doc);
                }

                Console.WriteLine("Done!");

                if (unhandled_tags.Count > 0)
                {
                    unhandled_tags.Sort();
                    Console.WriteLine("");
                    Console.WriteLine(
                        (
                        "Unhandled USFM tags: "
                        + string.Join(",", unhandled_tags)
                        + " ("
                        + unhandled_tags.Count().ToString()
                        + " total)"
                        )
                    );
                    if (!relaxed_conformance)
                        Console.WriteLine("Consider using the -r option for relaxed markup processing");

                }
            }
        }


        /// <summary>
        /// Reads the USFM file and stores information about which Bible book it
        /// represents and localized abbreviations in global variables.
        /// </summary>
        /// <param name="filename">a USFM filename</param>
        private void read_identifiers_from_osis(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            string osis = string.Empty;

            if (!string.IsNullOrEmpty(encodingStr))
            {
                try
                {
                    encoding = Encoding.GetEncoding(encodingStr);
                }
                catch
                {
                    encoding = Encoding.UTF8;
                }

                using (StreamReader sr = new StreamReader(filename, encoding))
                {
                    osis = sr.ReadToEnd().Trim() + "\n";
                }
            }
            else
            {
                encoding = Encoding.UTF8;
                using (StreamReader sr = new StreamReader(filename, encoding))
                {
                    osis = sr.ReadToEnd().Trim() + "\n";
                }
 
                Match match = Regex.Match(osis, @"\\ide\s+(.+)" + "\n");
                if (match.Success)
                { 
                    encodingStr = match.Groups[1].Value.ToLower().Trim();

                    if (!encodingStr.Equals("utf-8"))
                    {
                        if (aliases.Contains(encodingStr))
                        {
                            try
                            {
                                encoding = Encoding.GetEncoding(encodingStr);
                            }
                            catch
                            {
                                encoding = Encoding.UTF8;
                            }


                            using (StreamReader sr = new StreamReader(filename, encoding))
                            {
                                osis = sr.ReadToEnd().Trim() + "\n";
                            }
                        }
                        else
                        {
                            Console.WriteLine(("WARNING: Encoding \"" + encodingStr +
                               "\" unknown, processing " + filename + " as UTF-8"));
                        }
                    }
                }

                // keep a copy of the OSIS book abbreviation for below (\toc3 processing)
                // to store for mapping localized book names to/from OSIS
                string osis_book = string.Empty;
                match = Regex.Match(osis, @"\\id\s+([A-Z0-9]+)");
                if (match.Success)
                    osis_book = match.Groups[1].Value;

                if (!string.IsNullOrEmpty(osis_book))
                {
                    osis_book = BOOK_DICT[osis_book];
                    FILENAME_TO_OSIS[filename] = osis_book;
                }

                string loc_book = string.Empty;
                match = Regex.Match(osis, @"\\toc3\b\s+(.+)\s*");
                if (match.Success)
                    loc_book = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(loc_book))
                {
                    osis_to_loc_book[osis_book] = loc_book;
                    loc_to_osis_book[loc_book] = osis_book;
                }

            }
        }

        /// <summary>
        /// Prints usage statement.
        /// </summary>
        private void print_usage()
        {
            Console.WriteLine(
                (
                    "usfm2osis -- USFM "
                    + usfmVersion
                    + " to OSIS "
                    + osisVersion
                    + " converter version "
                    + scriptVersion
                )
            );
            Console.WriteLine("");
            Console.WriteLine("Usage: usfm2osis <osisWork> [OPTION] ...  <USFM filename|wildcard> ...");
            Console.WriteLine("");
            Console.WriteLine("  -h, --help       print this usage information");
            Console.WriteLine("  -d               debug mode (single-threaded, verbose output)");
            Console.WriteLine("  -e ENCODING      input encodingStr override (default is to read the USFM file's");
            Console.WriteLine("                     \\ide value or assume UTF-8 encodingStr in its absence)");
            Console.WriteLine("  -o FILENAME      output filename (default is: <osisWork>.osis.xml)");
            Console.WriteLine("  -r               enable relaxed markup processing (for non-standard USFM)");
            Console.WriteLine("  -s MODE          set book sorting mode: natural (default), alpha, canonical,");
            Console.WriteLine("                     usfm, random, none");
            Console.WriteLine("  -t NUM           set the number of separate processes to use (your maximum");
            Console.WriteLine("                     thread count by default)");
            Console.WriteLine("  -l LANG          set the language value to a BCP 47 code ('und' by default)");
            Console.WriteLine("  -v               verbose feedback");
            Console.WriteLine("  -x               disable XML validation");
            Console.WriteLine("");
            Console.WriteLine("As an example, if you want to generate the osisWork <Bible.KJV> and your USFM");
            Console.WriteLine("  are located in the ./KJV folder, enter:");
            Console.WriteLine("    python usfm2osis Bible.KJV ./KJV/*.usfm");
            verbose_print("", verbose);

            verbose_print("Supported encodings: " + string.Join(",", aliases), verbose);
        }
    }
}
