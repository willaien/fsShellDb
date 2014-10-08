using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace fsShellDb
{
    internal class Program
    {
        private static MongoClient _mongoClient;
        private static MongoGridFS _db;
        private static string _currentdirectory = "/";

        private static void Main()
        {
            var exit = false;
            _mongoClient = new MongoClient("mongodb://localhost");
            _db = new MongoGridFS(_mongoClient.GetServer(), "Default",
                new MongoGridFSSettings());
            while (!exit)
            {
                Console.Write(GetPrepend());
                var command = Console.ReadLine();
                Debug.Assert(command != null, "command != null");
                if (command.Length == 0) continue;
                //What the fuck?
                //Handles quotation marks in incoming commands
                var result = command.Split('"')
                    .Select((element, index) => index%2 == 0
                        ? element.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                        : new[] {element})
                    .SelectMany(element => element).ToList();
                switch (result[0].ToLower())
                {
                    case "exit":
                        exit = true;
                        break;
                    case "cd":
                        if (result.Count == 2)
                        {
                            if (result[1] == "..")
                            {
                                var sarray = _currentdirectory.Split('/').Where(x => x != "").ToArray();
                                var str = "/";
                                for (var i = 0; i < sarray.Length - 1; i++)
                                {
                                    str += sarray[i] + "/";
                                }
                                _currentdirectory = str;
                            }
                            else
                            {
                                _currentdirectory += result[1] + "/";
                            }
                        }
                        else
                        {
                            Console.WriteLine("  Usage: cd <dir>");
                        }
                        break;
                    case "server":
                        _mongoClient = new MongoClient("mongodb://" + result[1]);
                        _db = new MongoGridFS(_mongoClient.GetServer(), "Default",
                            new MongoGridFSSettings());
                        break;
                    case "db":
                        _db = new MongoGridFS(_mongoClient.GetServer(), result[1],
                            new MongoGridFSSettings());
                        break;
                    case "ls":
                        if (_db != null)
                        {
                            var query = from file in _db.FindAll()
                                where file.Name.StartsWith(_currentdirectory)
                                select file;
                            foreach (var file in query)
                                Console.WriteLine("  " + file.Name);
                        }
                        break;
                    case "put":
                        if (result.Count == 2)
                        {
                            var file = new FileInfo(result[1]);
                            if (!file.Exists)
                            {
                                Console.WriteLine("  File doesn't exist!");
                                break;
                            }
                            Console.Write("  Uploading " + file.Name + "...");
                            Debug.Assert(_db != null, "_db != null");
                            _db.Upload(file.FullName, _currentdirectory + file.Name);
                            Console.WriteLine("Done!");
                        }
                        else if (result.Count == 3)
                        {
                            var file = new FileInfo(result[1]);
                            if (!file.Exists)
                            {
                                Console.WriteLine("  File doesn't exist!");
                                break;
                            }
                            Console.Write("  Uploading " + file.Name + "...");
                            Debug.Assert(_db != null, "_db != null");
                            _db.Upload(file.FullName, result[2]);
                            Console.WriteLine("Done!");
                        }
                        else
                        {
                            Console.WriteLine("  Usage: put <src> [dest]");
                        }
                        break;
                    case "get":
                        if (result.Count == 2)
                        {
                            Debug.Assert(_db != null, "_db != null");
                            if (!_db.Exists(result[1]))
                            {
                                Console.WriteLine("  File doesn't exist!");
                                break;
                            }
                            Console.Write("  Downloading " + result[1] + "...");
                            _db.Download(result[1], result[1]);
                            Console.WriteLine("Done!");
                        }
                        else if (result.Count == 3)
                        {
                            Debug.Assert(_db != null, "_db != null");
                            if (!_db.Exists(result[1]))
                            {
                                Console.WriteLine("  File doesn't exist!");
                                break;
                            }
                            Console.Write("  Downloading " + result[1] + "...");
                            _db.Download(result[2], result[1]);
                            Console.WriteLine("Done!");
                        }
                        else
                        {
                            Console.WriteLine("  Usage: get <src> [dest]");
                        }
                        break;
                    case "del":
                        if (result.Count == 2)
                        {
                            Debug.Assert(_db != null, "_db != null");
                            if (!_db.Exists(result[1]))
                            {
                                Console.WriteLine("  File doesn't exist!");
                                break;
                            }
                            _db.Delete(result[1]);
                        }
                        else
                        {
                            Console.WriteLine("  Usage: del <filename>");
                        }
                        break;
                    default:
                        Console.WriteLine("  Invalid Command");
                        break;
                }
            }
        }

        private static string GetPrepend()
        {
            var str = "";
            if (_mongoClient == null) return str != "" ? str : ">";
            str += _db != null ? _db.DatabaseName + "@" : "";
            str += _mongoClient.GetServer().Settings.Server + ":" + _currentdirectory + ">";
            return str != "" ? str : ">";
        }
    }
}