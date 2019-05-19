using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace DocFxTocGenerate
{
    class Program
    {
        class TocItem
        {
            private List<TocItem> _items;

            public TocItem()
            {
            }
            public string name { get; set; }

            public string href { get; set; }
            public IReadOnlyCollection<TocItem> items => _items?.AsReadOnly();
            public void AddItem(TocItem item)
            {
                if (_items == null) { _items = new List<TocItem>(); }
                _items.Add(item);
            }
        }

        public static string rootFolder;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("use DocFxTocGenerate c:\foldername");
                return;
            }
            rootFolder = args[0];

            var tocRootItems = new TocItem();
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(rootFolder);
            WalkDirectoryTree(rootDir, tocRootItems);

            var serializer = new Serializer();
            var textWriter = new StringWriter();
            var output = serializer.Serialize(tocRootItems);

            System.IO.File.WriteAllText("output.yml", output);

            Console.WriteLine("Press a key to quit");
            Console.ReadKey();
        }


        static void WalkDirectoryTree(System.IO.DirectoryInfo folder, TocItem yamlNodes)
        {
            List<FileInfo> files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            files = GetFiles(folder, yamlNodes);

            // Now find all the subdirectories under this directory.
            subDirs = folder.GetDirectories();

            var subTocItemsList = new List<TocItem>();
            foreach (System.IO.DirectoryInfo dirInfo in subDirs)
            {
                if (dirInfo.Name.StartsWith(".")) { continue; }

                // Resursive call for each subdirectory.
                var newTocItem = new TocItem();
                var subFiles = dirInfo.GetFiles();
                if (subFiles.Length == 1 && dirInfo.GetDirectories().Length == 0)
                {
                    newTocItem.name = UppercaseFirst(subFiles[0].Name).Replace(".md", "");
                    newTocItem.href = GetRelativePath(subFiles[0].FullName, rootFolder);
                }
                else
                {
                    WalkDirectoryTree(dirInfo, newTocItem);
                    yamlNodes.AddItem(newTocItem);
                }
            }

        }

        private static List<FileInfo> GetFiles(DirectoryInfo folder, TocItem yamlNodes)
        {
            var files = folder.GetFiles("*.*").OrderBy(f => f.Name).ToList();
            if (files == null)
            {
                return null;
            }

            foreach (System.IO.FileInfo fi in files)
            {
                if (fi.Name.StartsWith(".")) { continue; }
                // TODO cleanup directory name
                yamlNodes.AddItem(new TocItem
                {
                    name = UppercaseFirst(fi.Name.Replace("-", " ")).Replace(".md", ""),
                    href = GetRelativePath(fi.FullName, rootFolder)
                });
            }


            return files;
        }

        static string GetRelativePath(string filePath, string sourcePath = null)
        {
            string currentDir = sourcePath ?? Environment.CurrentDirectory;
            DirectoryInfo directory = new DirectoryInfo(currentDir);
            FileInfo file = new FileInfo(filePath);

            string fullDirectory = directory.FullName;
            string fullFile = file.FullName;

            if (!fullFile.StartsWith(fullDirectory))
            {
                throw new InvalidOperationException("Unable to make relative path");
            }

            if (fullFile == fullDirectory) { return "/"; }

            // The +1 is to avoid the directory separator
            return fullFile.Substring(fullDirectory.Length + 1);
        }

        static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }
}
