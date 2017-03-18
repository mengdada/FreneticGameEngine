﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using LZ4;

namespace FreneticGameCore.Files
{
    /// <summary>
    /// Handles the file system cleanly.
    /// </summary>
    public class FileHandler
    {
        /// <summary>
        /// All PAK files known to the system.
        /// </summary>
        public List<PakFile> Paks = new List<PakFile>();

        /// <summary>
        /// All data files known to the system.
        /// </summary>
        public List<PakkedFile> Files = new List<PakkedFile>();

        /// <summary>
        /// The default text encoding.
        /// </summary>
        public static Encoding encoding = new UTF8Encoding(false);

        /// <summary>
        /// The base directory in which all data is stored.
        /// </summary>
        public string BaseDirectory = Environment.CurrentDirectory.Replace('\\', '/') + "/data/";

        /// <summary>
        /// All sub-directories used by the system.
        /// </summary>
        public List<string> SubDirectories = new List<string>();

        /// <summary>
        /// Loads a new subdirectory.
        /// </summary>
        /// <param name="dir">The directory name.</param>
        public void LoadDir(string dir)
        {
            string fdir = Environment.CurrentDirectory.Replace('\\', '/') + "/" + CleanFileName(dir) + "/";
            if (SubDirectories.Contains(fdir))
            {
                SysConsole.Output(OutputType.WARNING, "Ignoring attempt to add same directory twice.");
                return;
            }
            SubDirectories.Add(fdir);
            ClearAll();
            Init();
        }

        /// <summary>
        /// Clears away all file data.
        /// </summary>
        public void ClearAll()
        {
            foreach (PakFile pf in Paks)
            {
                pf.Storer.Dispose();
            }
            Paks.Clear();
            Files.Clear();
        }

        /// <summary>
        /// The current save directory.
        /// </summary>
        public string SaveDir = null;

        /// <summary>
        /// Call early in running to set a save directory prior to loading.
        /// </summary>
        /// <param name="dir">The save directory.</param>
        public void SetSaveDirEarly(string dir)
        {
            SaveDir = Environment.CurrentDirectory.Replace('\\', '/') + "/" + CleanFileName(dir) + "/";
            Directory.CreateDirectory(SaveDir);
            SubDirectories.Add(SaveDir);
        }

        /// <summary>
        /// Call late in running to set a save directory after having already loaded.
        /// </summary>
        /// <param name="dir">The save directory.</param>
        public void SetSaveDirLate(string dir)
        {
            SubDirectories.Clear();
            if (dir == null)
            {
                SaveDir = BaseDirectory;

            }
            else
            {
                dir = CleanFileName(dir);
                SaveDir = Environment.CurrentDirectory.Replace('\\', '/') + "/" + dir + "/";
                Directory.CreateDirectory(SaveDir);
                SubDirectories.Add(SaveDir);
            }
            ClearAll();
            Init();
        }

        /// <summary>
        /// Initialize the file system.
        /// </summary>
        public void Init()
        {
            foreach (string str in SubDirectories)
            {
                Load(str, Directory.GetFiles(str, "*.*", SearchOption.AllDirectories));
            }
            if (SaveDir == null)
            {
                SaveDir = BaseDirectory;
            }
            Load(BaseDirectory, Directory.GetFiles(BaseDirectory, "*.*", SearchOption.AllDirectories));
        }

        /// <summary>
        /// Load a set of files from a path.
        /// </summary>
        /// <param name="pth">The path.</param>
        /// <param name="allfiles">A list of all files that need to be loaded.</param>
        void Load(string pth, string[] allfiles)
        {
            foreach (string tfile in allfiles)
            {
                string file = tfile.Replace('\\', '/');
                if (file.Length == 0 || file[file.Length - 1] == '/')
                {
                    continue;
                }
                if (file.EndsWith(".pak"))
                {
                    Paks.Add(new PakFile(file.Replace(pth, "").ToLowerFast(), file));
                }
                else
                {
                    Files.Add(new PakkedFile(file.Replace(pth, "").ToLowerFast(), file));
                }
            }
            int id = 0;
            foreach (PakFile pak in Paks)
            {
                List<ZipStorer.ZipFileEntry> zents = pak.Storer.ReadCentralDir();
                pak.FileListIndex = Files.Count;
                foreach (ZipStorer.ZipFileEntry zent in zents)
                {
                    string name = zent.FilenameInZip.Replace('\\', '/').Replace("..", ".").Replace(":", "").ToLowerFast();
                    if (name.Length == 0 || name[name.Length - 1] == '/')
                    {
                        continue;
                    }
                    Files.Add(new PakkedFile(name, "", id, zent));
                }
                id++;
            }
        }

        /// <summary>
        /// Cleans a file name for direct system calls.
        /// </summary>
        /// <param name="input">The original file name.</param>
        /// <returns>The cleaned file name.</returns>
        public static string CleanFileName(string input)
        {
            StringBuilder output = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                // Remove double slashes, or "./"
                if ((input[i] == '/' || input[i] == '\\') && output.Length > 0 && (output[output.Length - 1] == '/' || output[output.Length - 1] == '.'))
                {
                    continue;
                }
                // Fix backslashes to forward slashes for cross-platform folders
                if (input[i] == '\\')
                {
                    output.Append('/');
                    continue;
                }
                // Remove ".." (up-a-level) folders, or "/."
                if (input[i] == '.' && output.Length > 0 && (output[output.Length - 1] == '.' || output[output.Length - 1] == '/'))
                {
                    continue;
                }
                // Clean spaces to underscores
                if (input[i] == (char)0x00A0 || input[i] == ' ')
                {
                    output.Append('_');
                    continue;
                }
                // Remove non-ASCII symbols, ASCII control codes, and Windows control symbols
                if (input[i] < 32 || input[i] > 126 || input[i] == '?' ||
                    input[i] == ':' || input[i] == '*' || input[i] == '|' ||
                    input[i] == '"' || input[i] == '<' || input[i] == '>' || input[i] == '#')
                {
                    output.Append('_');
                    continue;
                }
                // Lower-case letters only
                if (input[i] >= 'A' && input[i] <= 'Z')
                {
                    output.Append((char)(input[i] - ('A' - 'a')));
                    continue;
                }
                // All others normal
                output.Append(input[i]);
            }
            // Limit length
            if (output.Length > 100)
            {
                // Also, trim leading/trailing spaces.
                return output.ToString().Substring(0, 100).Trim();
            }
            // Also, trim leading/trailing spaces.
            return output.ToString().Trim();
        }

        /// <summary>
        /// Gets the index of a file by name.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <returns>The index, or -1 if not found.</returns>
        public int FileIndex(string filename)
        {
            string cleaned = CleanFileName(filename);
            for (int i = 0; i < Files.Count; i++)
            {
                if (Files[i].Name == cleaned)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns whether a file exists.
        /// </summary>
        /// <param name="filename">The name of the file to look for.</param>
        /// <returns>Whether the file exists.</returns>
        public bool Exists(string filename)
        {
            string cleaned = CleanFileName(filename);
            if (FileIndex(cleaned) != -1 || File.Exists(BaseDirectory + cleaned))
            {
                return true;
            }
            for (int i = 0; i < SubDirectories.Count; i++)
            {
                if (File.Exists(SubDirectories[i] + cleaned))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns all the byte data in a file.
        /// </summary>
        /// <param name="filename">The name of the file to read.</param>
        /// <returns>The file's data, as a byte array.</returns>
        public byte[] ReadBytes(string filename)
        {
            string fname = CleanFileName(filename);
            int ind = FileIndex(fname);
            if (ind == -1)
            {
                if (File.Exists(BaseDirectory + fname))
                {
                    return File.ReadAllBytes(BaseDirectory + fname);
                }
                else
                {
                    for (int i = 0; i < SubDirectories.Count; i++)
                    {
                        if (File.Exists(SubDirectories[i] + fname))
                        {
                            return File.ReadAllBytes(SubDirectories[i] + fname);
                        }
                    }
                    throw new UnknownFileException(fname);
                }
            }
            PakkedFile file = Files[ind];
            if (file.IsPakked)
            {
                MemoryStream ms = new MemoryStream();
                Paks[file.PakIndex].Storer.ExtractFile(file.Entry, ms);
                byte[] toret = ms.ToArray();
                ms.Close();
                return toret;
            }
            else
            {
                return File.ReadAllBytes(file.Handle);
            }
        }

        /// <summary>
        /// Returns a stream of the byte data in a file.
        /// </summary>
        /// <param name="filename">The name of the file to read.</param>
        /// <returns>The file's data, as a stream.</returns>
        public DataStream ReadToStream(string filename)
        {
            return new DataStream(ReadBytes(filename));
        }

        /// <summary>
        /// Returns all the text data in a file.
        /// </summary>
        /// <param name="filename">The name of the file to read.</param>
        /// <returns>The file's data, as a string.</returns>
        public string ReadText(string filename)
        {
            return encoding.GetString(ReadBytes(filename)).Replace("\r", "");
        }

        /// <summary>
        /// Returns a list of all folders that contain the filepath.
        /// </summary>
        public List<string> ListFolders(string filepath)
        {
            List<string> folds = new List<string>();
            string fname = "/" + CleanFileName("/" + filepath);
            while (fname.Contains("//"))
            {
                fname = fname.Replace("//", "/");
            }
            if (fname.EndsWith("/"))
            {
                fname = fname.Substring(0, fname.Length - 1);
            }
            string fn2 = fname + "/";
            if (fn2 == "//")
            {
                fn2 = "/";
            }
            for (int i = 0; i < Files.Count; i++)
            {
                string fina = "/" + Files[i].Name;
                if (fina.StartsWith(fn2))
                {
                    string fold = "/" + (Files[i].Name.LastIndexOf('/') <= 0 ? "" : Files[i].Name.Substring(0, Files[i].Name.LastIndexOf('/')));
                    if (fold.StartsWith(fn2) && !folds.Contains(fold))
                    {
                        folds.Add(fold);
                    }
                }
            }
            return folds;
        }

        /// <summary>
        /// Returns a list of all files inside a folder.
        /// </summary>
        public List<string> ListFiles(string filepath)
        {
            List<string> folds = new List<string>();
            string fname = "/" + CleanFileName("/" + filepath);
            while (fname.Contains("//"))
            {
                fname = fname.Replace("//", "/");
            }
            if (fname.EndsWith("/"))
            {
                fname = fname.Substring(0, fname.Length - 1);
            }
            string fn2 = fname + "/";
            if (fn2 == "//")
            {
                fn2 = "/";
            }
            for (int i = 0; i < Files.Count; i++)
            {
                string fina = "/" + Files[i].Name;
                if (fina.StartsWith(fn2))
                {
                    folds.Add(fina);
                }
            }
            return folds;
        }

        /// <summary>
        /// Creates a file system directory for a path.
        /// </summary>
        /// <param name="path">The path.</param>
        public void CreateDirectory(string path)
        {
            string fname = SaveDir + CleanFileName(path);
            if (!Directory.Exists(fname))
            {
                Directory.CreateDirectory(fname);
            }
        }

        /// <summary>
        /// Writes bytes to a file.
        /// </summary>
        /// <param name="filename">The name of the file to write to.</param>
        /// <param name="bytes">The byte data to write.</param>
        public void WriteBytes(string filename, byte[] bytes)
        {
            string fname = CleanFileName(filename);
            string finname;
            finname = SaveDir + fname;
            string dir = Path.GetDirectoryName(finname);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(finname, bytes);
            for (int i = 0; i < Files.Count; i++)
            {
                if (Files[i] == null)
                {
                    // TODO: Find out why this can happen
                    continue;
                }
                if (Files[i].Handle == finname)
                {
                    return;
                }
            }
            // TODO: Files.Insert(0, new PakkedFile(fname, finname));
        }

        /// <summary>
        /// Writes text to a file.
        /// </summary>
        /// <param name="filename">The name of the file to write to.</param>
        /// <param name="text">The text data to write.</param>
        public void WriteText(string filename, string text)
        {
            WriteBytes(filename, encoding.GetBytes(text.Replace('\r', ' ')));
        }

        /// <summary>
        /// Adds text to a file.
        /// </summary>
        /// <param name="filename">The name of the file to add to.</param>
        /// <param name="text">The text data to add.</param>
        public void AppendText(string filename, string text)
        {
            string textoutput = ReadText(filename);
            WriteText(filename, textoutput + text);
        }

        /// <summary>
        /// Compresses a byte array.
        /// </summary>
        /// <param name="input">Uncompressed data.</param>
        /// <returns>Compressed data.</returns>
        public static byte[] Compress(byte[] input)
        {
            MemoryStream memstream = new MemoryStream();
            LZ4Stream lzstream = new LZ4Stream(memstream, LZ4StreamMode.Compress);
            lzstream.Write(input, 0, input.Length);
            lzstream.Close();
            byte[] finaldata = memstream.ToArray();
            memstream.Close();
            return finaldata;
        }

        /// <summary>
        /// Decompress a byte array.
        /// </summary>
        /// <param name="input">Compressed data.</param>
        /// <returns>Uncompressed data.</returns>
        public static byte[] Uncompress(byte[] input)
        {
            using (MemoryStream output = new MemoryStream())
            {
                MemoryStream memstream = new MemoryStream(input);
                LZ4Stream LZStream = new LZ4Stream(memstream, LZ4StreamMode.Decompress);
                LZStream.CopyTo(output);
                LZStream.Close();
                memstream.Close();
                return output.ToArray();
            }
        }

        /// <summary>
        /// Compresses a byte array using the GZip algorithm.
        /// </summary>
        /// <param name="input">Uncompressed data.</param>
        /// <returns>Compressed data.</returns>
        public static byte[] GZip(byte[] input)
        {
            MemoryStream memstream = new MemoryStream();
            GZipStream GZStream = new GZipStream(memstream, CompressionMode.Compress);
            GZStream.Write(input, 0, input.Length);
            GZStream.Close();
            byte[] finaldata = memstream.ToArray();
            memstream.Close();
            return finaldata;
        }

        /// <summary>
        /// Decompress a byte array using the GZip algorithm.
        /// </summary>
        /// <param name="input">Compressed data.</param>
        /// <returns>Uncompressed data.</returns>
        public static byte[] UnGZip(byte[] input)
        {
            using (MemoryStream output = new MemoryStream())
            {
                MemoryStream memstream = new MemoryStream(input);
                GZipStream GZStream = new GZipStream(memstream, CompressionMode.Decompress);
                GZStream.CopyTo(output);
                GZStream.Close();
                memstream.Close();
                return output.ToArray();
            }
        }
    }

    /// <summary>
    /// Represents a file available to the <see cref="FileHandler"/>.
    /// </summary>
    public class PakkedFile
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name = null;

        /// <summary>
        /// The full path of the file.
        /// </summary>
        public string Handle = null;

        /// <summary>
        /// Whether the file is in a PAK file.
        /// </summary>
        public bool IsPakked = false;

        /// <summary>
        /// The index in a PAK file, or -1.
        /// </summary>
        public int PakIndex = -1;

        /// <summary>
        /// The PAK file inner file object.
        /// </summary>
        public ZipStorer.ZipFileEntry Entry;

        /// <summary>
        /// Constructs a pakked file.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="handle">The system file path.</param>
        public PakkedFile(string name, string handle)
        {
            Name = name;
            Handle = handle;
        }

        /// <summary>
        /// Constructs a pakked file.
        /// </summary>
        /// <param name="name">The file name.</param>
        /// <param name="handle">The PAK file path.</param>
        /// <param name="index">The PAK index.</param>
        /// <param name="entry">The PAK entry.</param>
        public PakkedFile(string name, string handle, int index, ZipStorer.ZipFileEntry entry)
        {
            Name = name;
            Handle = handle;
            IsPakked = true;
            PakIndex = index;
            Entry = entry;
        }
    }

    /// <summary>
    /// Represents a PAK file for use by the <see cref="FileHandler"/>.
    /// </summary>
    public class PakFile
    {
        /// <summary>
        /// The name of the PAK file.
        /// </summary>
        public string Name = null;

        /// <summary>
        /// The path of the PAK file.
        /// </summary>
        public string Handle = null;

        /// <summary>
        /// The PAK file object.
        /// </summary>
        public ZipStorer Storer = null;

        /// <summary>
        /// The index in the file list.
        /// </summary>
        public int FileListIndex = 0;

        /// <summary>
        /// Constructs the PAK file.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="handle">The path of the file.</param>
        public PakFile(string name, string handle)
        {
            Handle = handle;
            Name = name;
            Storer = ZipStorer.Open(handle, FileAccess.Read);
        }
    }
}