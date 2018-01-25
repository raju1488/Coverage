﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Palmmedia.ReportGenerator.Common;
using Palmmedia.ReportGenerator.Logging;
using Palmmedia.ReportGenerator.Parser.Analysis;
using Palmmedia.ReportGenerator.Properties;

namespace Palmmedia.ReportGenerator.Parser
{
    /// <summary>
    /// Parser for XML reports generated by PartCover 2.3 and above.
    /// </summary>
    internal class PartCover23Parser : ParserBase
    {
        /// <summary>
        /// Regex to analyze if a method name belongs to a lamda expression.
        /// </summary>
        private const string LambdaMethodRegex = "<.+>.+__";

        /// <summary>
        /// The Logger.
        /// </summary>
        private static readonly ILogger Logger = LoggerFactory.GetLogger(typeof(PartCover23Parser));

        /// <summary>
        /// Dictionary containing the assembly names by id.
        /// In PartCover 2.3.0.35109 the assemblies are referenced by an id.
        /// Before only their name was required.
        /// </summary>
        private Dictionary<string, string> assembliesByIdDictionary;

        /// <summary>
        /// Dictionary containing the file ids by the file's path.
        /// </summary>
        private Dictionary<string, string> fileIdByFilenameDictionary;

        /// <summary>
        /// The type elements of the report.
        /// </summary>
        private XElement[] types;

        /// <summary>
        /// The file elements of the report.
        /// </summary>
        private XElement[] files;

        /// <summary>
        /// The attribute name to the corresponding assembly.
        /// In PartCover 2.3.0.35109 this is "asmref".
        /// </summary>
        private string assemblyAttribute = "asm";

        /// <summary>
        /// Initializes a new instance of the <see cref="PartCover23Parser"/> class.
        /// </summary>
        /// <param name="report">The report file as XContainer.</param>
        internal PartCover23Parser(XContainer report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            this.types = report.Descendants("Type").ToArray();
            this.files = report.Descendants("File").ToArray();

            // Determine which version of PartCover 2.3 has been used.
            // In PartCover 2.3.0.35109 the assemblies are referenced by an id and the attribute name in Type elements has changed.
            var assemblies = report.Descendants("Assembly");
            if (assemblies.Any() && assemblies.First().Attribute("id") != null)
            {
                this.assemblyAttribute = "asmref";
                this.assembliesByIdDictionary = assemblies.ToDictionary(a => a.Attribute("id").Value, a => a.Attribute("name").Value);
            }
            else
            {
                this.assembliesByIdDictionary = assemblies.ToDictionary(a => a.Attribute("name").Value, a => a.Attribute("name").Value);
            }

            this.fileIdByFilenameDictionary = this.files.ToDictionary(f => f.Attribute("url").Value, f => f.Attribute("id").Value);

            var assemblyNames = this.assembliesByIdDictionary.Values
                .Distinct()
                .OrderBy(a => a)
                .ToArray();

            Parallel.ForEach(assemblyNames, assemblyName => this.AddAssembly(this.ProcessAssembly(assemblyName)));

            this.types = null;
            this.files = null;
            this.assembliesByIdDictionary = null;
            this.fileIdByFilenameDictionary = null;
        }

        /// <summary>
        /// Extracts the methods/properties of the given <see cref="XElement">XElements</see>.
        /// </summary>
        /// <param name="codeFile">The code file.</param>
        /// <param name="fileId">The id of the file.</param>
        /// <param name="methods">The methods.</param>
        private static void SetCodeElements(CodeFile codeFile, string fileId, IEnumerable<XElement> methods)
        {
            foreach (var method in methods)
            {
                if (Regex.IsMatch(method.Attribute("name").Value, LambdaMethodRegex))
                {
                    continue;
                }

                string sig = method.Attribute("sig").Value;
                string methodName = method.Attribute("name").Value + sig.Substring(sig.LastIndexOf('('));

                CodeElementType type = CodeElementType.Method;

                if (methodName.StartsWith("get_", StringComparison.OrdinalIgnoreCase)
                    || methodName.StartsWith("set_", StringComparison.OrdinalIgnoreCase))
                {
                    type = CodeElementType.Property;
                    methodName = methodName.Substring(4);
                }

                var seqpnt = method
                    .Elements("pt")
                    .Where(pt => pt.HasAttributeWithValue("fid", fileId))
                    .FirstOrDefault();

                if (seqpnt != null)
                {
                    int line = int.Parse(seqpnt.Attribute("sl").Value, CultureInfo.InvariantCulture);
                    codeFile.AddCodeElement(new CodeElement(methodName, type, line));
                }
            }
        }

        /// <summary>
        /// Processes the given assembly.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <returns>The <see cref="Assembly"/>.</returns>
        private Assembly ProcessAssembly(string assemblyName)
        {
            Logger.DebugFormat("  " + Resources.CurrentAssembly, assemblyName);

            var classNames = this.types
                .Where(type => this.assembliesByIdDictionary[type.Attribute(this.assemblyAttribute).Value].Equals(assemblyName)
                    && !Regex.IsMatch(type.Attribute("name").Value, "<.*>.+__"))
                .Select(type => type.Attribute("name").Value)
                .OrderBy(name => name)
                .Distinct()
                .ToArray();

            var assembly = new Assembly(assemblyName);

            Parallel.ForEach(classNames, className => assembly.AddClass(this.ProcessClass(assembly, className)));

            return assembly;
        }

        /// <summary>
        /// Processes the given class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="className">Name of the class.</param>
        /// <returns>The <see cref="Class"/>.</returns>
        private Class ProcessClass(Assembly assembly, string className)
        {
            var fileIdsOfClass = this.types
                .Where(type => this.assembliesByIdDictionary[type.Attribute(this.assemblyAttribute).Value].Equals(assembly.Name)
                    && (type.Attribute("name").Value.Equals(className, StringComparison.Ordinal)
                        || type.Attribute("name").Value.StartsWith(className + "<", StringComparison.Ordinal)))
                .Elements("Method")
                .Elements("pt")
                .Where(pt => pt.Attribute("fid") != null)
                .Select(pt => pt.Attribute("fid").Value)
                .Distinct().ToHashSet();

            var filesOfClass = this.files
                .Where(file => fileIdsOfClass.Contains(file.Attribute("id").Value))
                .Select(file => file.Attribute("url").Value)
                .ToArray();

            var @class = new Class(className, assembly);

            foreach (var file in filesOfClass)
            {
                @class.AddFile(this.ProcessFile(@class, file));
            }

            return @class;
        }

        /// <summary>
        /// Processes the file.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>The <see cref="CodeFile"/>.</returns>
        private CodeFile ProcessFile(Class @class, string filePath)
        {
            string fileId = this.fileIdByFilenameDictionary[filePath];

            var methods = this.types
                .Where(type => this.assembliesByIdDictionary[type.Attribute(this.assemblyAttribute).Value].Equals(@class.Assembly.Name)
                    && (type.Attribute("name").Value.Equals(@class.Name, StringComparison.Ordinal)
                        || type.Attribute("name").Value.StartsWith(@class.Name + "<", StringComparison.Ordinal)))
                .Elements("Method")
                .ToArray();

            var seqpntsOfFile = methods.Elements("pt")
                .Where(seqpnt => seqpnt.HasAttributeWithValue("fid", fileId))
                .Select(seqpnt => new
                {
                    LineNumberStart = int.Parse(seqpnt.Attribute("sl").Value, CultureInfo.InvariantCulture),
                    LineNumberEnd = seqpnt.Attribute("el") != null ? int.Parse(seqpnt.Attribute("el").Value, CultureInfo.InvariantCulture) : int.Parse(seqpnt.Attribute("sl").Value, CultureInfo.InvariantCulture),
                    Visits = int.Parse(seqpnt.Attribute("visit").Value, CultureInfo.InvariantCulture)
                })
                .OrderBy(seqpnt => seqpnt.LineNumberEnd)
                .ToArray();

            int[] coverage = new int[] { };
            LineVisitStatus[] lineVisitStatus = new LineVisitStatus[] { };

            if (seqpntsOfFile.Length > 0)
            {
                coverage = new int[seqpntsOfFile[seqpntsOfFile.LongLength - 1].LineNumberEnd + 1];
                lineVisitStatus = new LineVisitStatus[seqpntsOfFile[seqpntsOfFile.LongLength - 1].LineNumberEnd + 1];

                for (int i = 0; i < coverage.Length; i++)
                {
                    coverage[i] = -1;
                }

                foreach (var seqpnt in seqpntsOfFile)
                {
                    for (int lineNumber = seqpnt.LineNumberStart; lineNumber <= seqpnt.LineNumberEnd; lineNumber++)
                    {
                        coverage[lineNumber] = coverage[lineNumber] == -1 ? seqpnt.Visits : coverage[lineNumber] + seqpnt.Visits;
                        lineVisitStatus[lineNumber] = lineVisitStatus[lineNumber] == LineVisitStatus.Covered || seqpnt.Visits > 0 ? LineVisitStatus.Covered : LineVisitStatus.NotCovered;
                    }
                }
            }

            var codeFile = new CodeFile(filePath, coverage, lineVisitStatus);

            SetCodeElements(codeFile, fileId, methods);

            return codeFile;
        }
    }
}
