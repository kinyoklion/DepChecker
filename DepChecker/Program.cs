using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace DepChecker
{
    class Program
    {
        private static readonly List<AssemblySummary> DependencyTree = new();
        private static readonly Dictionary<KeyValuePair<string, Version>, HashSet<string>> Issues = new();
        private static readonly Dictionary<KeyValuePair<string, Version>, HashSet<string>> Redirects = new();

        static void CollectDependencies(string path, Assembly loadedAssembly, AssemblySummary summary)
        {
            var referencedAssemblies = loadedAssembly.GetReferencedAssemblies();

            foreach (var assemblyName in referencedAssemblies)
            {
                var fileName = Path.Combine(path, assemblyName.Name + ".dll");

                var fileExists = File.Exists(fileName);

                if (fileExists)
                {
                    try
                    {
                        var dependentLoadedAssembly = Assembly.LoadFile(fileName);
                        var dependentAssemblySummary = ProcessLoadedAssembly(Source.File, loadedAssembly, summary,
                            dependentLoadedAssembly, assemblyName);
                        CollectDependencies(path, dependentLoadedAssembly, dependentAssemblySummary);
                    }
                    catch (Exception)
                    {
                        summary.Dependencies.Add(new AssemblySummary
                        {
                            AssemblyName = assemblyName,
                            Dependencies = new List<AssemblySummary>(), Exists = false,
                            Source = Source.NotFound
                        });
                        AddIssue(loadedAssembly, assemblyName);
                    }
                }
                else
                {
                    try
                    {
                        var dependentLoadedAssembly = Assembly.Load(assemblyName.FullName);

                        if (dependentLoadedAssembly.GetName().Version != assemblyName.Version)
                        {
                            AddRedirect(dependentLoadedAssembly.GetName(), loadedAssembly, assemblyName);
                        }

                        ProcessLoadedAssembly(Source.Runtime, loadedAssembly, summary, dependentLoadedAssembly,
                            assemblyName);

                        continue;
                    }
                    catch (Exception)
                    {
                        WriteLineColor(ConsoleColor.Red, $"Failed to load {assemblyName.FullName}");
                    }

                    var dependentAssemblySummary = new AssemblySummary
                    {
                        AssemblyName = assemblyName,
                        Dependencies = new List<AssemblySummary>(), Exists = false,
                        Source = Source.NotFound
                    };
                    AddIssue(loadedAssembly, assemblyName);
                    summary.Dependencies.Add(dependentAssemblySummary);
                }
            }
        }

        private static void AddIssue(Assembly loadedAssembly, AssemblyName assemblyName)
        {
            var key = new KeyValuePair<string, Version>(assemblyName.Name, assemblyName.Version);
            if (!Issues.ContainsKey(key))
            {
                Issues[key] = new HashSet<string>();
            }

            Issues[key].Add(loadedAssembly.FullName);
        }

        private static void AddRedirect(AssemblyName found, Assembly loadedAssembly, AssemblyName assemblyName)
        {
            var key = new KeyValuePair<string, Version>(assemblyName.Name, assemblyName.Version);
            if (!Redirects.ContainsKey(key))
            {
                Redirects[key] = new HashSet<string>();
            }

            Redirects[key].Add($"{loadedAssembly.FullName} \r\n\t\t  Version Expected: {found.FullName}");
        }


        private static AssemblySummary ProcessLoadedAssembly(Source source, Assembly loadedAssembly,
            AssemblySummary summary,
            Assembly dependentLoadedAssembly, AssemblyName assemblyName)
        {
            var dependentLoadedAssemblyName = dependentLoadedAssembly.GetName();
            var versionMatches = assemblyName.Version == dependentLoadedAssemblyName.Version;

            if (!versionMatches)
            {
                AddIssue(loadedAssembly, assemblyName);
            }

            var dependentAssemblySummary = new AssemblySummary
            {
                AssemblyName = assemblyName,
                Dependencies = new List<AssemblySummary>(),
                Exists = versionMatches,
                Source = versionMatches ? source : Source.IncorrectVersion
            };
            summary.Dependencies.Add(dependentAssemblySummary);
            return dependentAssemblySummary;
        }

        static void WriteLineColor(ConsoleColor color, string text)
        {
            var consoleColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = consoleColor;
        }

        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please specify the path to inspect.");
                return 1;
            }

            var path = Path.GetFullPath(args[0]);
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Path not valid. Please verify that the path is a directory and it exists.");
                return 1;
            }

            var files = Directory.GetFiles(path, "*.dll");
            if (files.Length == 0)
            {
                Console.WriteLine("No files to inspect.");
                return 0;
            }

            foreach (var file in files)
            {
                try
                {
                    var loadedAssembly = Assembly.LoadFile(file);
                    var assemblyName = loadedAssembly.GetName();

                    var summary = new AssemblySummary
                    {
                        Dependencies = new List<AssemblySummary>(),
                        Exists = true,
                        AssemblyName = assemblyName,
                    };
                    DependencyTree.Add(summary);
                    CollectDependencies(path, loadedAssembly, summary);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Encountered issue with file {file}: {e}");
                }
            }

            PrintDependencies(DependencyTree, 0);
            PrintRedirects();
            PrintIssues();

            return Issues.Count;
        }

        private static void PrintDependencies(List<AssemblySummary> summaries, int depth)
        {
            foreach (var summary in summaries)
            {
                var summaryString = summary.ToString();
                WriteLineColor(summary.Exists ? ConsoleColor.Green : ConsoleColor.Red,
                    summary.ToString().PadLeft(depth * 4 + summaryString.Length));
                PrintDependencies(summary.Dependencies, depth + 1);
            }
        }

        private static void PrintIssues()
        {
            foreach (var (key, value) in Issues)
            {
                WriteLineColor(ConsoleColor.Red, $"Could not locate {key}:");
                Console.WriteLine("Expected by:");
                foreach (var dependent in value)
                {
                    Console.WriteLine("\t" + dependent);
                }
            }
        }

        private static void PrintRedirects()
        {
            foreach (var (key, value) in Redirects)
            {
                WriteLineColor(ConsoleColor.Yellow, $"Assembly Loaded Via Redirect {key}:");
                Console.WriteLine("Expected by:");
                foreach (var dependent in value)
                {
                    Console.WriteLine("\t" + dependent);
                }
            }
        }
    }
}