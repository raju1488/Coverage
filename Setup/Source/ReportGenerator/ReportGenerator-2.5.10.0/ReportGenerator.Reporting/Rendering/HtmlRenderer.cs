using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Palmmedia.ReportGenerator.Parser.Analysis;
using Palmmedia.ReportGenerator.Properties;
using System.Configuration;

namespace Palmmedia.ReportGenerator.Reporting.Rendering
{
    /// <summary>
    /// HTML report renderer.
    /// </summary>
    internal class HtmlRenderer : RendererBase, IReportRenderer, IDisposable
    {
        #region HTML Snippets

        /// <summary>
        /// The head of each generated HTML file.
        /// </summary>
        private const string HtmlStart = @"<!DOCTYPE html>
<html data-ng-app=""coverageApp"">
<head>
<meta charset=""utf-8"" />
<meta http-equiv=""X-UA-Compatible"" content=""IE=EDGE,chrome=1"" />
<title>{0} - {1}</title>
{2}
</head><body data-ng-controller=""{3}"">
<div id=""msg"">
</div>
<div class=""container"" style=""display:none;"" id=""body"">
<div class=""containerleft"">";

        /// <summary>
        /// The end of each generated HTML file.
        /// </summary>
        private const string HtmlEnd = @"</div></div>
{0}
</body></html>";

        /// <summary>
        /// The link to the static CSS file.
        /// </summary>
        private const string CssLink = "<link rel=\"stylesheet\" type=\"text/css\" href=\"report.css\" />";

        #endregion

        /// <summary>
        /// Dictionary containing the filenames of the class reports by class.
        /// </summary>
        private static readonly Dictionary<string, string> FileNameByClass = new Dictionary<string, string>();

        /// <summary>
        /// Indicates that only a summary report is created (no class reports).
        /// </summary>
        private readonly bool onlySummary;

        /// <summary>
        /// Indicates that CSS and JavaScript is included into the HTML instead of seperate files.
        /// </summary>
        private readonly bool inlineCssAndJavaScript;

        /// <summary>
        /// Contains report specific JavaScript content.
        /// </summary>
        private readonly StringBuilder javaScriptContent;

        /// <summary>
        /// The report builder.
        /// </summary>
        private TextWriter reportTextWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlRenderer" /> class.
        /// </summary>
        /// <param name="onlySummary">if set to <c>true</c> only a summary report is created (no class reports).</param>
        /// <param name="inlineCssAndJavaScript">if set to <c>true</c> CSS and JavaScript is included into the HTML instead of seperate files.</param>
        /// <param name="javaScriptContent">StringBuilder used to collect report specific JavaScript.</param>
        internal HtmlRenderer(bool onlySummary, bool inlineCssAndJavaScript, StringBuilder javaScriptContent)
        {
            this.onlySummary = onlySummary;
            this.inlineCssAndJavaScript = inlineCssAndJavaScript;
            this.javaScriptContent = javaScriptContent;
        }

        /// <summary>
        /// Begins the summary report.
        /// </summary>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="title">The title.</param>
        public void BeginSummaryReport(string targetDirectory, string fileName, string title)
        {
            string targetPath = Path.Combine(targetDirectory, this.onlySummary ? "summary.htm" : "index.htm");

            if (fileName != null)
            {
                targetPath = Path.Combine(targetDirectory, fileName);
            }

            this.CreateTextWriter(targetPath);

            using (var cssStream = this.GetCombinedCss())
            {
                string style = this.inlineCssAndJavaScript ?
                    "<style TYPE=\"text/css\">" + new StreamReader(cssStream).ReadToEnd() + "</style>"
                    : CssLink;

                this.reportTextWriter.WriteLine(HtmlStart, WebUtility.HtmlEncode(title), WebUtility.HtmlEncode(ReportResources.CoverageReport), style, "SummaryViewCtrl");
            }
        }

        /// <summary>
        /// Begins the class report.
        /// </summary>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="className">Name of the class.</param>
        public void BeginClassReport(string targetDirectory, string assemblyName, string className)
        {
            string fileName = GetClassReportFilename(assemblyName, className);

            this.CreateTextWriter(Path.Combine(targetDirectory, fileName));
            using (var cssStream = this.GetCombinedCss())
            {
                string style = this.inlineCssAndJavaScript ?
                    "<style TYPE=\"text/css\">" + new StreamReader(cssStream).ReadToEnd() + "</style>"
                    : CssLink;

                this.reportTextWriter.WriteLine(HtmlStart, WebUtility.HtmlEncode(className), WebUtility.HtmlEncode(ReportResources.CoverageReport), style, "DetailViewCtrl");
            }
        }

        /// <summary>
        /// Adds a header to the report.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Header(string text)
        {
            this.reportTextWriter.WriteLine("<h1>{0}</h1>", WebUtility.HtmlEncode(text));
        }

        /// <summary>
        /// Adds the test methods to the report.
        /// </summary>
        /// <param name="testMethods">The test methods.</param>
        /// <param name="codeElementsByFileIndex">Code elements by file index.</param>
        public void TestMethods(IEnumerable<TestMethod> testMethods, IDictionary<int, IEnumerable<CodeElement>> codeElementsByFileIndex)
        {
            if (testMethods == null)
            {
                throw new ArgumentNullException(nameof(testMethods));
            }

            if (!testMethods.Any() && codeElementsByFileIndex.Count == 0)
            {
                return;
            }

            // Close 'containerleft' and begin 'containerright'
            this.reportTextWriter.WriteLine("</div>");
            this.reportTextWriter.WriteLine("<div class=\"containerright\">");
            this.reportTextWriter.WriteLine("<div class=\"containerrightfixed\">");

            if (testMethods.Any())
            {
                this.reportTextWriter.WriteLine("<h1>{0}</h1>", WebUtility.HtmlEncode(ReportResources.Testmethods));

                int counter = 0;

                this.reportTextWriter.WriteLine(
                    "<label title=\"{0}\"><input type=\"radio\" name=\"method\" value=\"AllTestMethods\" data-ng-change=\"switchTestMethod('AllTestMethods')\" data-ng-model=\"selectedTestMethod\" />{0}</label>",
                    WebUtility.HtmlEncode(ReportResources.All),
                    counter);

                foreach (var testMethod in testMethods)
                {
                    counter++;
                    this.reportTextWriter.WriteLine(
                        "<br /><label title=\"{0}\"><input type=\"radio\" name=\"method\" value=\"M{1}\" data-ng-change=\"switchTestMethod('M{1}')\" data-ng-model=\"selectedTestMethod\" />{2}</label>",
                        WebUtility.HtmlEncode(testMethod.Name),
                        testMethod.Id,
                        WebUtility.HtmlEncode(testMethod.ShortName));
                }
            }

            if (codeElementsByFileIndex.Count > 0)
            {
                this.reportTextWriter.WriteLine("<h1>{0}</h1>", WebUtility.HtmlEncode(ReportResources.MethodsProperties));

                foreach (var item in codeElementsByFileIndex)
                {
                    foreach (var codeElement in item.Value)
                    {
                        this.reportTextWriter.WriteLine(
                            "<a class=\"{0}\" href=\"#file{1}_line{2}\" data-ng-click=\"navigateToHash('#file{1}_line{2}')\" title=\"{3}\">{3}</a><br />",
                            codeElement.CodeElementType == CodeElementType.Method ? "method" : "property",
                            item.Key,
                            codeElement.Line,
                            WebUtility.HtmlEncode(codeElement.Name));
                    }
                }
            }

            this.reportTextWriter.WriteLine("<br/></div>");
        }

        /// <summary>
        /// Adds a file of a class to a report.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        public void File(string path)
        {
            this.reportTextWriter.WriteLine("<h2 id=\"{0}\">{1}</h2>", WebUtility.HtmlEncode(HtmlRenderer.ReplaceNonLetterChars(path)), WebUtility.HtmlEncode(path));
        }

        /// <summary>
        /// Adds a paragraph to the report.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Paragraph(string text)
        {
            this.reportTextWriter.WriteLine("<p>{0}</p>", WebUtility.HtmlEncode(text));
        }

        /// <summary>
        /// Adds a table with two columns to the report.
        /// </summary>
        public void BeginKeyValueTable()
        {

            this.reportTextWriter.WriteLine("<table class=\"overview table-fixed\">");
            this.reportTextWriter.WriteLine("<colgroup>");
            this.reportTextWriter.WriteLine("<col class=\"column250\" />");
            this.reportTextWriter.WriteLine("<col />");
            this.reportTextWriter.WriteLine("</colgroup>");
            this.reportTextWriter.WriteLine("<tbody>");
        }
        public void BeginKeyValueTable1()
        {
            this.reportTextWriter.WriteLine(@"<table align=""left"" class="" zui-table zui-table-zebra zui-table-horizontal"" width=""50% "" >

                    <thead id=""tableHead"">
                        <tr class=""odd"">
                            <th>
                                <h2 style=""float:left;""> Code Coverage</h2>
                            </th>
                            <th></th>
                            <!--<td>Data</td>-->
                        </tr>
                    </thead>
                    <tbody id=""tableBody"">");
        }
        public void BeginKeyValueTable2()
        {
            this.reportTextWriter.WriteLine(@"<table align=""right"" class=""zui-table zui-table-zebra zui-table-horizontal"" height=""377.7px"" width=""49% ""> <!--important-->

                    <thead id =""table1"">
                        <tr>
                            <th><h2>Build Coverage</h2></th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody id=""table2"">");
        }

        public void Begindiv(string buildversion, string value)
        {
            this.reportTextWriter.WriteLine(@"<div>
        <div class="" boxednew"" style=""padding-left: 5px;"">
            <div style = ""height:30px"" >
                
                <img src=""data:image/png;base64,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"" width=""160"" height=""40"" id=""img5"" style=""border: 2px solid white; border-image: none; width: 143px; float: left;background-color: white"" >
                <h4 style = ""left: 0px; width: 90%; text-align: center;"" > Report Generation</h4>
            </div>

            <h5> Build : " + buildversion + @"</h5>
            
        </div> 
    </div> 
    <div>
            <div class=""boxed"">
                <img id=""img"" src=""data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAB4AAAAeCAYAAAA7MK6iAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAKKSURBVEhLrZdPa9RQFMWTTMaZYcAWF25cKOjOQhezsNJiUBAEKR0X6W66aa1rXVfiStFvUJGC4h/QUpRCoStdufQjCCIIIoJLQWw855EX0js3L2bSC79m8s6752QybyavXla+g2KdAJfAOkjAfXAXrIIFMAWKpflZPD+KolASx3GLIqvVai36vr8NfoDUwXfwCvOvZa2e5k2ojQWbDhQMrsDoY8G4Du/DMJynT5qmasahQQ6w0PhQGE1EEAT8SEzJnDzYqJ53DA1vpUFDXsA3oPlYcJIkFDDHf1doOEoY7jEnD7YLCeIjMdnFU7TM4LgjxkvBbd9gTp7HP1gIkTa5DLtycYw1vYSDdrs9YJ+tAIOfxCQnCByaxiAYabqDDyaRBZPrygQn6LnBXgSvaLoL3F3+CJnPtvYqbhIMuD68abz4JYRKGgZ/Y2+tRWVpGJyy95YmVIHgOAte1fQq2HtPE8AzaEsIGPLdSXq93il+HzudzhlNB1z1Q/i8Eb4GBj9QhIN+v3+SoqsGg0E7e1lauLCzir8JTjQB7EAb4VaSFUm32z2dveNzmg5G7IfPnvA1MPimJlQB42U247im6VWwd0ETquDnmAVPvKqP48VPKVTRMPgre/nL9VoIlTQMfmKCYXJVEZ00CcZv9ZwJZmGg1t4KwZM+nfZNIMo+jy8ok0pBsHkeI3hZ00v4g+fxefax8h0ITMq+0xrbaLmIo/o91YD/beZoey7e8pey4Yh4TH++yUPB9gTF3chz0dSUTRoX99c8z08K4bztG2j4Kwzq8hs+dzLL8n21hVdGIVtw+8Lsf9nFQpqlj93SFuH4WLCFIgsr+DKMtsCXgrHGZ7CJCzb/urA03yiKwn902r1+I8rzxwAAAABJRU5ErkJggg=="" width=""30"" height=""30"" style=""float:right;""  /> 
                <b>Coverage Data&nbsp;:</b>&nbsp;&nbsp;
                <b>" + value + @"</b>
            </div>

            <div id = ""wrapper"" >");
        }
        public void Enddiv()
        {
            this.reportTextWriter.WriteLine(@" </div>
        </div>");
        }
        /// <summary>
        /// Adds a summary table to the report.
        /// </summary>
        /// <param name="branchCoverageAvailable">if set to <c>true</c> branch coverage is available.</param>
        public void BeginSummaryTable(bool branchCoverageAvailable)
        {
            this.reportTextWriter.WriteLine("<div data-ng-if=\"filteringEnabled\" data-reactive-table data-assemblies=\"assemblies\" data-branch-coverage-available=\"branchCoverageAvailable\"></div>");

            this.reportTextWriter.WriteLine("<div data-ng-if=\"!filteringEnabled\">");
            this.reportTextWriter.WriteLine(
                "<div class=\"ng-hide customizebox\" data-ng-show=\"true\"><input data-ng-click=\"enableFiltering()\" value=\"{0}\" title=\"{1}\" type=\"submit\" /></div>",
                WebUtility.HtmlEncode(ReportResources.ShowCustomizeBox),
                WebUtility.HtmlEncode(ReportResources.ShowCustomizeBoxHelp));
            this.reportTextWriter.WriteLine("</div>");

            this.reportTextWriter.WriteLine("<table data-ng-if=\"!filteringEnabled\" class=\"overview table\" id=\"m\">");
            this.reportTextWriter.WriteLine("<colgroup>");
            this.reportTextWriter.WriteLine("<col />");
            this.reportTextWriter.WriteLine("<col class=\"column70\" />");
            this.reportTextWriter.WriteLine("<col class=\"column50\" />");
            this.reportTextWriter.WriteLine("<col class=\"column50\" />");
            this.reportTextWriter.WriteLine("<col class=\"column50\" />");
            this.reportTextWriter.WriteLine("<col class=\"column50\" />");
            this.reportTextWriter.WriteLine("<col class=\"column70\" />");
            this.reportTextWriter.WriteLine("<col class=\"column60\" />");
            this.reportTextWriter.WriteLine("<col class=\"column60\" />");
            this.reportTextWriter.WriteLine("<col class=\"column60\" />");
            this.reportTextWriter.WriteLine("<col class=\"column80\" />");
            this.reportTextWriter.WriteLine("<col class=\"column112\" />");

            //if (branchCoverageAvailable)
            //{
            //    this.reportTextWriter.WriteLine("<col class=\"column80\" />");
            //    this.reportTextWriter.WriteLine("<col class=\"column112\" />");
            //}

            //this.reportTextWriter.WriteLine("</colgroup>");

            //this.reportTextWriter.Write(
            //    "<thead><tr><th>{0}</th><th class=\"right\">{1}</th><th class=\"right\">{2}</th><th class=\"right\">{3}</th><th class=\"right\">{4}</th><th class=\"right\">{5}</th><th class=\"right\">{6}</th><th class=\"right\">{7}</th><th class=\"center\" colspan=\"2\">{8}</th>",
            //    WebUtility.HtmlEncode(ReportResources.Name),
            //    WebUtility.HtmlEncode(ReportResources.Covered),
            //    WebUtility.HtmlEncode(ReportResources.Uncovered),
            //    WebUtility.HtmlEncode(ReportResources.Coverable),
            //    WebUtility.HtmlEncode(ReportResources.Total),

            //    WebUtility.HtmlEncode(ReportResources.NewLines),
            //    WebUtility.HtmlEncode(ReportResources.TestedNewLines),
            //    WebUtility.HtmlEncode(ReportResources.NewCoverage),
            //WebUtility.HtmlEncode(ReportResources.TestCoverage),

            //WebUtility.HtmlEncode(ReportResources.Coverage));
            //if (branchCoverageAvailable)
            //{
            //    this.reportTextWriter.Write(
            //    "<th class=\"center\" colspan=\"2\">{0}</th>",
            //    WebUtility.HtmlEncode(ReportResources.BranchCoverage));
            //}

            //this.reportTextWriter.WriteLine("</tr></thead>");
            //this.reportTextWriter.WriteLine("<tbody>");
        }

        /// <summary>
        /// Adds custom summary elements to the report.
        /// </summary>
        /// <param name="assemblies">The assemblies.</param>
        /// <param name="branchCoverageAvailable">if set to <c>true</c> branch coverage is available.</param>
        public void CustomSummary(IEnumerable<Assembly> assemblies, bool branchCoverageAvailable)
        {
            System.Diagnostics.Debugger.Launch();
            this.javaScriptContent.AppendLine();
            this.javaScriptContent.AppendLine();
            this.javaScriptContent.AppendLine(@"
                                                var inc = 10;
                                             var ctmp = true;
                                             var codesum = [];
                                             var mydata = [];
                                             var xhr = new XMLHttpRequest();
                                             var MongoAPICount = ""http://172.26.6.58:28017/CodeData/$cmd/?filter_count=test_collection&limit=1"";
        var MongoAPI = ""http://172.26.6.58:28017/CodeData/test_collection/?filter_ID="";

            xhr.open(""GET"", MongoAPICount, false);
            xhr.send();
            var datacount = JSON.parse(xhr.response);
            var cc = datacount.rows[0].n;   
            //xhr.open(""GET"", MongoAPI, false);
            //xhr.send();
            //data = JSON.parse(xhr.response);
            data = GetMongoDataSum();
            mydata = [];
            codesum = data.rows[0];
            mydata.push(codesum.asmbly);
            codesum.asmbly = [];
            for (x = 1; x <= cc; x++)
            {
                xhr.open(""GET"", MongoAPI + x, false);
                xhr.send();
                data = JSON.parse(xhr.response);
                if (data.rows.length > 0)
                    mydata.push(data.rows[0]);
            }");
            this.javaScriptContent.AppendLine();

            this.javaScriptContent.AppendLine("var branchCoverageAvailable = " + branchCoverageAvailable.ToString().ToLowerInvariant() + ";");

            #region mognoinsert
            StringBuilder sb = new StringBuilder();
            StringBuilder sumsb = new StringBuilder();

            sumsb.AppendFormat("    \"asmblycount\" : \"{0}\",", assemblies.Count());
            sumsb.AppendFormat("    \"classescount\" : \"{0}\",", assemblies.Sum(z => z.Classes.Count()));
            sumsb.AppendFormat("    \"files\" : \"{0}\",", assemblies.Sum(z => z.Classes.Sum(w => w.Files.Count())));
            sumsb.AppendFormat("    \"modifiedasmblycount\" : \"{0}\",", assemblies.Select(z => z.Newlines > 0).Count());

            sumsb.AppendFormat("    \"coveredLines\" : \"{0}\",", assemblies.Sum(z => z.Classes.Sum(x => x.CoveredLines)));
            sumsb.AppendFormat("    \"uncoveredLines\" : \"{0}\",", assemblies.Sum(z => (z.Classes.Sum(x => x.CoverableLines))) - assemblies.Sum(z => (z.Classes.Sum(x => x.CoveredLines))));
            sumsb.AppendFormat("    \"coverableLines\" : \"{0}\",", assemblies.Sum(z => z.Classes.Sum(x => x.CoverableLines)));
            if (assemblies.Sum(z => z.Classes.Sum(x => x.CoverableLines)) != 0)
            {
                sumsb.AppendFormat("    \"linecoverage\" : \"{0}\",", (100 * (assemblies.Sum(z => z.Classes.Sum(x => x.CoveredLines))) / (assemblies.Sum(z => z.Classes.Sum(x => x.CoverableLines)))));
            }
            else
                sumsb.AppendFormat("    \"linecoverage\" : \"{0}\",", "0");

            if (assemblies.Sum(z => z.Classes.Sum(x => x.CoverableLines)) != 0)
            {
                sumsb.AppendFormat("    \"branchCoverage\" : \"{0}\",", (100 * (assemblies.Sum(z => z.Classes.Sum(x => x.CoveredBranches.GetValueOrDefault()))) / (assemblies.Sum(z => z.Classes.Sum(x => x.TotalBranches.GetValueOrDefault())))));
            }
            else
                sumsb.AppendFormat("    \"branchCoverage\" : \"{0}\",", "0");

            sumsb.AppendFormat("    \"totalLines\" : \"{0}\",", assemblies.Sum(z => z.Classes.Sum(x => x.TotalLines)));
            sumsb.AppendFormat("    \"newlines\" : \"{0}\",", assemblies.Sum(z => z.Classes.Sum(x => x.NewLines)));
            sumsb.AppendFormat("    \"testednewlines\" : \"{0}\",", assemblies.Sum(z => z.Classes.Sum(x => x.TestedNewLines)));

            if (assemblies.Sum(z => z.Classes.Sum(x => x.NewLines)) != 0)
            {
                sumsb.AppendFormat("    \"buildcoverage\" : \"{0}\",", (100 * (assemblies.Sum(z => z.Classes.Sum(x => x.TestedNewLines))) / (assemblies.Sum(z => z.Classes.Sum(x => x.NewLines)))));
            }
            else
                sumsb.AppendFormat("    \"buildcoverage\" : \"{0}\",", "0");

            sumsb.AppendLine("    \"asmbly\" : [");
            //sb.AppendLine("{");
            var lastasm = assemblies.LastOrDefault();
            foreach (var assembly in assemblies)
            {
                sumsb.Append("    { ");
                sumsb.AppendFormat("    \"name\" : \"{0}\",", assembly.Name);
                sumsb.AppendFormat("    \"coveredLines\" : \"{0}\",", assembly.Classes.Sum(x => x.CoveredLines));
                sumsb.AppendFormat("    \"uncoveredLines\" : \"{0}\",", assembly.Classes.Sum(x => x.CoverableLines) - assembly.Classes.Sum(x => x.CoveredLines));
                sumsb.AppendFormat("    \"coverableLines\" : \"{0}\",", assembly.Classes.Sum(x => x.CoverableLines));
                sumsb.AppendFormat("    \"totalLines\" : \"{0}\",", assembly.Classes.Sum(x => x.TotalLines));
                sumsb.AppendFormat("    \"newlines\" : \"{0}\",", assembly.Classes.Sum(x => x.NewLines));
                sumsb.AppendFormat("    \"testednewlines\" : \"{0}\",", assembly.Classes.Sum(x => x.TestedNewLines));

                sumsb.AppendFormat("    \"coverageType\" : \"{0}\",", "LineCoverage");

                if (assembly.Classes.Sum(x => x.NewLines) != 0)
                {
                    sumsb.AppendFormat("    \"testcoverage\" : \"{0}\",", (100 * assembly.Classes.Sum(x => x.TestedNewLines)) / assembly.Classes.Sum(x => x.NewLines));
                }
                else
                    sumsb.AppendFormat("    \"testcoverage\" : \"{0}\",", "0");

                if (assembly.Classes.Sum(x => x.CoverableLines) != 0)
                {
                    sumsb.AppendFormat("    \"coverage\" : \"{0}\",", (100 * assembly.Classes.Sum(x => x.CoveredLines)) / assembly.Classes.Sum(x => x.CoverableLines));
                }
                else
                    sumsb.AppendFormat("    \"coverage\" : \"{0}\",", "0");

                if (assembly.Classes.Sum(x => x.TotalBranches) != 0)
                {
                    sumsb.AppendFormat("    \"branchCoverage\" : \"{0}\"", (100 * assembly.Classes.Sum(x => x.CoveredBranches)) / assembly.Classes.Sum(x => x.TotalBranches));
                }
                else
                    sumsb.AppendFormat("    \"branchCoverage\" : \"{0}\"", "0");


                if (assembly.Equals(lastasm))
                {
                    sumsb.AppendLine("}");
                }
                else
                    sumsb.AppendLine("},");

                //sb.Append("    { ");
                sb = new StringBuilder();
                sb.AppendFormat("    \"name\" : \"{0}\",", assembly.Name);
                sb.AppendLine("    \"buildchanges\" : [");
                var last = assembly.Classes.LastOrDefault();
                foreach (var @class in assembly.Classes)
                {
                    var buildlineslist = "[\"" + string.Join("\",\"", @class.BuildLinesList) + "\"]";
                    var buildtestedlineslist = "[\"" + string.Join("\",\"", @class.BuildTestedLinesList) + "\"]";
                    sb.Append("    { ");
                    sb.AppendFormat(" \"name\" : \"{0}\",", @class.Name);
                    sb.AppendFormat(" \"buildlines\" : {0},", buildlineslist);
                    sb.AppendFormat(" \"buildtestedlines\" : {0}", buildtestedlineslist);
                    if (@class.Equals(last))
                    {
                        sb.AppendLine(" }");
                    }
                    else
                        sb.AppendLine(" },");
                }

                //if (assembly.Equals(lastasm))
                //{
                //    sb.AppendLine("	]");
                //}
                //else
                sb.AppendLine("	],");



                sb.AppendLine("    \"classes\" : [");

                foreach (var @class in assembly.Classes)
                {
                    var historicCoverages = this.FilterHistoricCoverages(@class.HistoricCoverages, 10);


                    //var buildlineslist = "[" + string.Join(",", @class.BuildLinesList) + "]";

                    var lineCoverageHistory = "[" + string.Join(",", historicCoverages.Select(h => h.CoverageQuota.GetValueOrDefault().ToString(CultureInfo.InvariantCulture))) + "]";
                    var branchCoverageHistory = "[]";
                    if (historicCoverages.Any(h => h.BranchCoverageQuota.HasValue))
                    {
                        branchCoverageHistory = "[" + string.Join(",", historicCoverages.Select(h => h.BranchCoverageQuota.GetValueOrDefault().ToString(CultureInfo.InvariantCulture))) + "]";
                    }

                    sb.Append("    { ");
                    sb.AppendFormat(" \"name\" : \"{0}\",", @class.Name);
                    sb.AppendFormat(
                        " \"reportPath\" : \"{0}\",",
                        this.onlySummary ? string.Empty : GetClassReportFilename(@class.Assembly.ShortName, @class.Name));
                    sb.AppendFormat(" \"coveredLines\" : {0},", @class.CoveredLines);
                    sb.AppendFormat(" \"uncoveredLines\" : {0},", @class.CoverableLines - @class.CoveredLines);
                    sb.AppendFormat(" \"coverableLines\" : {0} ,", @class.CoverableLines);
                    sb.AppendFormat(" \"totalLines\" : {0},", @class.TotalLines.GetValueOrDefault());

                    sb.AppendFormat(" \"newlines\" : {0},", @class.NewLines);//
                    sb.AppendFormat(" \"testednewlines\" : {0},", @class.TestedNewLines);//

                    //sb.AppendFormat(" \"buildlines\" : {0},", buildlineslist);

                    //sb.AppendFormat(" \"newcoverage\" : \"{0}\",", @class.NewCoverage + "%");
                    if (@class.NewLines != 0)
                    {
                        sb.AppendFormat("    \"testcoverage\" : \"{0}\",", (100 * @class.TestedNewLines / @class.NewLines));
                    }
                    else
                        sb.AppendFormat("    \"testcoverage\" : \"{0}\",", "0");
                    if (@class.CoverableLines != 0)
                    {
                        sb.AppendFormat("    \"coverage\" : \"{0}\",", (100 * @class.CoveredLines / @class.CoverableLines));
                    }
                    else
                        sb.AppendFormat("    \"coverage\" : \"{0}\",", "0");
                    if (@class.TotalBranches != 0)
                    {
                        sb.AppendFormat("    \"branchCoverage\" : \"{0}\",", (100 * @class.CoveredBranches / @class.TotalBranches));
                    }
                    else
                        sb.AppendFormat("    \"branchCoverage\" : \"{0}\",", "0");


                    //sb.AppendFormat(" \"testcoverage\" : \"{0}\",", @class.NewCoverage + "%");
                    sb.AppendFormat(" \"coverageType\" : \"{0}\",", @class.CoverageType);
                    sb.AppendFormat(
                        " \"methodCoverage\" : {0},",
                        @class.CoverageType == CoverageType.MethodCoverage && @class.CoverageQuota.HasValue ? @class.CoverageQuota.Value.ToString(CultureInfo.InvariantCulture) : "\"-\"");
                    sb.AppendFormat(" \"coveredBranches\" : {0},", @class.CoveredBranches.GetValueOrDefault());
                    sb.AppendFormat(" \"totalBranches\" : {0},", @class.TotalBranches.GetValueOrDefault());
                    sb.AppendFormat(" \"lineCoverageHistory\" : {0},", lineCoverageHistory);
                    sb.AppendFormat(" \"branchCoverageHistory\" : {0}", branchCoverageHistory);
                    if (@class.Equals(last))
                    {
                        sb.AppendLine(" }");
                    }
                    else
                        sb.AppendLine(" },");
                }

                //if (assembly.Equals(lastasm))
                //{
                //    sb.AppendLine("  ]");
                //}
                //else
                sb.AppendLine("  ],");

                //sb.AppendLine("}");

                System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFrom("ReportHelper.dll");
                if (asm != null)
                {

                    string s = "{" + sb.ToString() + "}";
                    Type type = asm.GetType("Report.ReportHelper");
                    if (type != null && !string.IsNullOrEmpty(s))
                    {
                        object[] methodData = null;
                        methodData = new object[1];
                        methodData[0] = s;
                        object obj = Activator.CreateInstance(type);
                        type.InvokeMember("MongoInsert", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod, null, obj, methodData);
                    }
                }

            }
            sumsb.AppendLine("	]");

            System.Reflection.Assembly asm1 = System.Reflection.Assembly.LoadFrom("ReportHelper.dll");
            if (asm1 != null)
            {
                string ss = "{" + sumsb.ToString() + "}";
                Type type = asm1.GetType("Report.ReportHelper");
                if (type != null && !string.IsNullOrEmpty(ss))
                {
                    object[] methodData = null;
                    methodData = new object[1];
                    methodData[0] = ss;
                    object obj = Activator.CreateInstance(type);
                    type.InvokeMember("MongoInsertSum", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod, null, obj, methodData);
                }
            }

            //sb.AppendLine("}");

            #endregion mongoinsert



        }

        /// <summary>
        /// Adds a metrics table to the report.
        /// </summary>
        /// <param name="metric">The metric.</param>
        public void BeginMetricsTable(MethodMetric metric)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            this.reportTextWriter.WriteLine("<table class=\"overview table-fixed\">");
            this.reportTextWriter.Write("<thead><tr>");

            this.reportTextWriter.Write("<th>{0}</th>", WebUtility.HtmlEncode(ReportResources.Method));

            foreach (var met in metric.Metrics)
            {
                if (met.ExplanationUrl == null)
                {
                    this.reportTextWriter.Write("<th>{0}</th>", WebUtility.HtmlEncode(met.Name));
                }
                else
                {
                    this.reportTextWriter.Write("<th>{0} <a href=\"{1}\" class=\"info\">&nbsp;</a></th>", WebUtility.HtmlEncode(met.Name), WebUtility.HtmlEncode(met.ExplanationUrl.OriginalString));
                }
            }

            this.reportTextWriter.WriteLine("</tr></thead>");
            this.reportTextWriter.WriteLine("<tbody>");
        }

        /// <summary>
        /// Adds a file analysis table to the report.
        /// </summary>
        /// <param name="headers">The headers.</param>
        public void BeginLineAnalysisTable(IEnumerable<string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            this.reportTextWriter.WriteLine("<table class=\"lineAnalysis\">");
            this.reportTextWriter.Write("<thead><tr>");

            foreach (var header in headers)
            {
                this.reportTextWriter.Write("<th>{0}</th>", WebUtility.HtmlEncode(header));
            }

            this.reportTextWriter.WriteLine("</tr></thead>");
            this.reportTextWriter.WriteLine("<tbody>");
        }

        /// <summary>
        /// Adds a table row with two cells to the report.
        /// </summary>
        /// <param name="key">The text of the first column.</param>
        /// <param name="value">The text of the second column.</param>
        public void KeyValueRow(string key, string value)
        {
            this.reportTextWriter.WriteLine(
                "<tr><th>{0}</th><td>{1}</td></tr>",
                WebUtility.HtmlEncode(key),
                WebUtility.HtmlEncode(value));
        }

        /// <summary>
        /// Adds a table row with two cells to the report.
        /// </summary>
        /// <param name="key">The text of the first column.</param>
        /// <param name="files">The files.</param>
        public void KeyValueRow(string key, IEnumerable<string> files)
        {
            string value = string.Join("<br />", files.Select(v => string.Format(CultureInfo.InvariantCulture, "<a href=\"#{0}\" data-ng-click=\"navigateToHash('#{0}')\">{1}</a>", WebUtility.HtmlEncode(ReplaceNonLetterChars(v)), WebUtility.HtmlEncode(v))));

            this.reportTextWriter.WriteLine(
                "<tr><th>{0}</th><td>{1}</td></tr>",
                WebUtility.HtmlEncode(key),
                value);
        }

        /// <summary>
        /// Adds the given metric values to the report.
        /// </summary>
        /// <param name="metric">The metric.</param>
        public void MetricsRow(MethodMetric metric)
        {
            if (metric == null)
            {
                throw new ArgumentNullException(nameof(metric));
            }

            this.reportTextWriter.Write("<tr>");

            this.reportTextWriter.Write("<td title=\"{0}\">{1}</td>", WebUtility.HtmlEncode(metric.Name), WebUtility.HtmlEncode(metric.ShortName));

            foreach (var metricValue in metric.Metrics.Select(m => m.Value))
            {
                this.reportTextWriter.Write("<td>{0}</td>", metricValue.ToString(CultureInfo.InvariantCulture));
            }

            this.reportTextWriter.WriteLine("</tr>");
        }

        /// <summary>
        /// Adds the coverage information of a single line of a file to the report.
        /// </summary>
        /// <param name="fileIndex">The index of the file.</param>
        /// <param name="analysis">The line analysis.</param>
        public void LineAnalysis(int fileIndex, LineAnalysis analysis)
        {
            if (analysis == null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            string formattedLine = analysis.LineContent
                .Replace(((char)11).ToString(), "  ") // replace tab
                .Replace(((char)9).ToString(), "  "); // replace tab

            if (formattedLine.Length > 120)
            {
                formattedLine = formattedLine.Substring(0, 120);
            }

            formattedLine = WebUtility.HtmlEncode(formattedLine);
            formattedLine = formattedLine.Replace(" ", "&nbsp;");

            string lineVisitStatus = ConvertToCssClass(analysis.LineVisitStatus, false);

            string title = null;
            if (analysis.CoveredBranches.HasValue && analysis.TotalBranches.HasValue && analysis.TotalBranches.Value > 0)
            {
                title = string.Format(WebUtility.HtmlEncode(ReportResources.CoveredBranches), analysis.CoveredBranches, analysis.TotalBranches);
            }

            if (title != null)
            {
                this.reportTextWriter.Write("<tr title=\"{0}\" data-coverage=\"{{", title);
            }
            else
            {
                this.reportTextWriter.Write("<tr data-coverage=\"{");
            }

            this.reportTextWriter.Write(
                "'AllTestMethods': {{'VC': '{0}', 'LVS': '{1}'}}",
                analysis.LineVisitStatus != LineVisitStatus.NotCoverable ? analysis.LineVisits.ToString(CultureInfo.InvariantCulture) : string.Empty,
                lineVisitStatus);

            foreach (var coverageByTestMethod in analysis.LineCoverageByTestMethod)
            {
                this.reportTextWriter.Write(
                    ", 'M{0}': {{'VC': '{1}', 'LVS': '{2}'}}",
                    coverageByTestMethod.Key.Id.ToString(CultureInfo.InvariantCulture),
                    coverageByTestMethod.Value.LineVisitStatus != LineVisitStatus.NotCoverable ? coverageByTestMethod.Value.LineVisits.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    ConvertToCssClass(coverageByTestMethod.Value.LineVisitStatus, false));
            }

            this.reportTextWriter.Write("}\">");

            this.reportTextWriter.Write(
                "<td class=\"{0}\">&nbsp;</td>",
                lineVisitStatus);
            this.reportTextWriter.Write(
                "<td class=\"leftmargin rightmargin right\">{0}</td>",
                analysis.LineVisitStatus != LineVisitStatus.NotCoverable ? analysis.LineVisits.ToString(CultureInfo.InvariantCulture) : string.Empty);
            this.reportTextWriter.Write(
                "<td class=\"rightmargin right\"><a id=\"file{0}_line{1}\"></a><code>{1}</code></td>",
                fileIndex,
                analysis.LineNumber);

            if (title != null)
            {
                int branchCoverage = (int)(100 * (double)analysis.CoveredBranches.Value / analysis.TotalBranches.Value);
                branchCoverage -= branchCoverage % 10;
                this.reportTextWriter.Write("<td class=\"branch{0}\">&nbsp;</td>", branchCoverage);
            }
            else
            {
                this.reportTextWriter.Write("<td></td>");
            }

            this.reportTextWriter.Write(
                "<td class=\"{0}\"><code>{1}</code></td>",
               ConvertToCssClass(analysis.LineVisitStatus, true),
                formattedLine);
            this.reportTextWriter.WriteLine("</tr>");
        }

        /// <summary>
        /// Finishes the current table.
        /// </summary>
        public void FinishTable()
        {
            this.reportTextWriter.WriteLine("</tbody>");
            this.reportTextWriter.WriteLine("</table>");
        }

        /// <summary>
        /// Charts the specified historic coverages.
        /// </summary>
        /// <param name="historicCoverages">The historic coverages.</param>
        public void Chart(IEnumerable<HistoricCoverage> historicCoverages)
        {
            if (historicCoverages == null)
            {
                throw new ArgumentNullException(nameof(historicCoverages));
            }

            string id = Guid.NewGuid().ToString("N");

            this.reportTextWriter.WriteLine("<div id=\"mainHistoryChart\" class=\"ct-chart\" data-history-chart data-data=\"historyChartData{0}\"></div>", id);

            historicCoverages = this.FilterHistoricCoverages(historicCoverages, 100);

            var series = new List<string>();
            series.Add("[" + string.Join(",", historicCoverages.Select(h => h.CoverageQuota.GetValueOrDefault().ToString(CultureInfo.InvariantCulture))) + "]");

            if (historicCoverages.Any(h => h.BranchCoverageQuota.HasValue))
            {
                series.Add("[" + string.Join(",", historicCoverages.Select(h => h.BranchCoverageQuota.GetValueOrDefault().ToString(CultureInfo.InvariantCulture))) + "]");
            }

            var toolTips = historicCoverages.Select(h =>
                string.Format(
                    "'<h3>{0} - {1}</h3>{2}{3}{4}'",
                    h.ExecutionTime.ToShortDateString(),
                    h.ExecutionTime.ToLongTimeString(),
                    h.CoverageQuota.HasValue ? string.Format(CultureInfo.InvariantCulture, "<br /><span class=\"linecoverage\"></span> {0} {1}% ({2}/{3})", WebUtility.HtmlEncode(ReportResources.Coverage2), h.CoverageQuota.Value, h.CoveredLines, h.CoverableLines) : null,
                    h.BranchCoverageQuota.HasValue ? string.Format(CultureInfo.InvariantCulture, "<br /><span class=\"branchcoverage\"></span> {0} {1}% ({2}/{3})", WebUtility.HtmlEncode(ReportResources.BranchCoverage2), h.BranchCoverageQuota.Value, h.CoveredBranches, h.TotalBranches) : null,
                    string.Format(CultureInfo.InvariantCulture, "<br />{0} {1}", WebUtility.HtmlEncode(ReportResources.TotalLines), h.TotalLines)));

            this.javaScriptContent.AppendFormat("var historyChartData{0} = {{", id);
            this.javaScriptContent.AppendLine();
            this.javaScriptContent.AppendFormat(
                "    \"series\" : [{0}],",
                string.Join(",", series));
            this.javaScriptContent.AppendLine();

            this.javaScriptContent.AppendFormat(
                 "    \"tooltips\" : [{0}]",
                 string.Join(",", toolTips));
            this.javaScriptContent.AppendLine();
            this.javaScriptContent.AppendLine("};");
            this.javaScriptContent.AppendLine();
        }

        /// <summary>
        /// Adds the coverage information of an assembly to the report.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="branchCoverageAvailable">if set to <c>true</c> branch coverage is available.</param>
        public void SummaryAssembly(Assembly assembly, bool branchCoverageAvailable)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            this.reportTextWriter.Write("<tr>");
            this.reportTextWriter.Write("<th>{0}</th>", WebUtility.HtmlEncode(assembly.Name));
            this.reportTextWriter.Write("<th class=\"right\">{0}</th>", assembly.CoveredLines);
            this.reportTextWriter.Write("<th class=\"right\">{0}</th>", assembly.CoverableLines - assembly.CoveredLines);
            this.reportTextWriter.Write("<th class=\"right\">{0}</th>", assembly.CoverableLines);
            this.reportTextWriter.Write("<th class=\"right\">{0}</th>", assembly.TotalLines.GetValueOrDefault());

            this.reportTextWriter.Write("<th class=\"right\">{0}</th>", assembly.Newlines);
            this.reportTextWriter.Write("<th class=\"right\">{0}</th>", assembly.TestedNewLines);
            this.reportTextWriter.Write("<th class=\"right\">{0}</th>", assembly.NewCoverage + "%");

            this.reportTextWriter.Write(
                "<th title=\"{0}\" class=\"right\">{1}</th>",
                assembly.CoverageQuota.HasValue ? CoverageType.LineCoverage.ToString() : string.Empty,
                assembly.CoverageQuota.HasValue ? assembly.CoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
            this.reportTextWriter.Write("<th>{0}</th>", CreateCoverageTable(assembly.CoverageQuota));

            if (branchCoverageAvailable)
            {
                this.reportTextWriter.Write(
                "<th class=\"right\">{0}</th>",
                assembly.BranchCoverageQuota.HasValue ? assembly.BranchCoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
                this.reportTextWriter.Write("<th>{0}</th>", CreateCoverageTable(assembly.BranchCoverageQuota));
            }

            this.reportTextWriter.WriteLine("</tr>");
        }

        /// <summary>
        /// Adds the coverage information of a class to the report.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <param name="branchCoverageAvailable">if set to <c>true</c> branch coverage is available.</param>
        public void SummaryClass(Class @class, bool branchCoverageAvailable)
        {
            if (@class == null)
            {
                throw new ArgumentNullException(nameof(@class));
            }

            string filenameColumn = @class.Name;

            if (!this.onlySummary)
            {
                filenameColumn = string.Format(
                    CultureInfo.InvariantCulture,
                    "<a href=\"{0}\">{1}</a>",
                    WebUtility.HtmlEncode(GetClassReportFilename(@class.Assembly.ShortName, @class.Name)),
                    WebUtility.HtmlEncode(@class.Name));
            }

            this.reportTextWriter.Write("<tr>");
            this.reportTextWriter.Write("<td>{0}</td>", filenameColumn);
            this.reportTextWriter.Write("<td class=\"right\">{0}</td>", @class.CoveredLines);
            this.reportTextWriter.Write("<td class=\"right\">{0}</td>", @class.CoverableLines - @class.CoveredLines);
            this.reportTextWriter.Write("<td class=\"right\">{0}</td>", @class.CoverableLines);

            this.reportTextWriter.Write("<td class=\"right\">{0}</td>", @class.CoverableLines - @class.CoveredLines);
            this.reportTextWriter.Write("<td class=\"right\">{0}</td>", @class.CoverableLines);
            this.reportTextWriter.Write(
                "<td title=\"{0}\" class=\"right\">{1}</td>",
                @class.CoverageType,
                @class.CoverageQuota.HasValue ? @class.CoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
            this.reportTextWriter.Write("<td>{0}</td>", CreateCoverageTable(@class.CoverageQuota));

            this.reportTextWriter.Write("<td class=\"right\">{0}</td>", @class.TotalLines.GetValueOrDefault());
            this.reportTextWriter.Write(
                "<td title=\"{0}\" class=\"right\">{1}</td>",
                @class.CoverageType,
                @class.CoverageQuota.HasValue ? @class.CoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
            this.reportTextWriter.Write("<td>{0}</td>", CreateCoverageTable(@class.CoverageQuota));

            if (branchCoverageAvailable)
            {
                this.reportTextWriter.Write(
                    "<td class=\"right\">{0}</td>",
                    @class.BranchCoverageQuota.HasValue ? @class.BranchCoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
                this.reportTextWriter.Write("<td>{0}</td>", CreateCoverageTable(@class.BranchCoverageQuota));
            }

            this.reportTextWriter.WriteLine("</tr>");
        }

        /// <summary>
        /// Adds the footer to the report.
        /// </summary>
        public void AddFooter()
        {
            System.Diagnostics.Debugger.Launch();
            this.reportTextWriter.Write(string.Format(
                CultureInfo.InvariantCulture,
                "<div class=\"footer\">{0} {1} {2} <h1>Lorenzo</h1></div>",
                WebUtility.HtmlEncode(ReportResources.GeneratedOn),
                //typeof(IReportBuilder).Assembly.GetName().Name,
                //typeof(IReportBuilder).Assembly.GetName().Version,
                DateTime.Now.ToShortDateString(),
                DateTime.Now.ToLongTimeString()));
        }

        /// <summary>
        /// Saves a summary report.
        /// </summary>
        /// <param name="targetDirectory">The target directory.</param>
        public void SaveSummaryReport(string targetDirectory)
        {
            this.SaveReport();

            if (!this.inlineCssAndJavaScript)
            {
                this.SaveCss(targetDirectory);
                this.SaveJavaScript(targetDirectory);
            }
        }

        /// <summary>
        /// Saves a class report.
        /// </summary><param name="targetDirectory">The target directory.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="className">Name of the class.</param>
        public void SaveClassReport(string targetDirectory, string assemblyName, string className)
        {
            this.SaveReport();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.reportTextWriter != null)
                {
                    this.reportTextWriter.Dispose();
                }
            }
        }

        /// <summary>
        /// Builds a table showing the coverage quota with red and green bars.
        /// </summary>
        /// <param name="coverage">The coverage quota.</param>
        /// <returns>Table showing the coverage quota with red and green bars.</returns>
        private static string CreateCoverageTable(decimal? coverage)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("<table class=\"coverage\"><tr>");

            if (coverage.HasValue)
            {
                int covered = (int)Math.Round(coverage.Value, 0);
                int uncovered = 100 - covered;

                if (covered > 0)
                {
                    stringBuilder.Append("<td class=\"green covered" + covered + "\">&nbsp;</td>");
                }

                if (uncovered > 0)
                {
                    stringBuilder.Append("<td class=\"red covered" + uncovered + "\">&nbsp;</td>");
                }
            }
            else
            {
                stringBuilder.Append("<td class=\"gray covered100\">&nbsp;</td>");
            }

            stringBuilder.Append("</tr></table>");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the <see cref="LineVisitStatus" /> to the corresponding CSS class.
        /// </summary>
        /// <param name="lineVisitStatus">The line visit status.</param>
        /// <param name="lightcolor">if set to <c>true</c> a CSS class representing a light color is returned.</param>
        /// <returns>The corresponding CSS class.</returns>
        private static string ConvertToCssClass(LineVisitStatus lineVisitStatus, bool lightcolor)
        {
            switch (lineVisitStatus)
            {
                case LineVisitStatus.Covered:
                    return lightcolor ? "lightgreen" : "green";
                case LineVisitStatus.NotCovered:
                    return lightcolor ? "lightred" : "red";
                case LineVisitStatus.PartiallyCovered:
                    return lightcolor ? "lightorange" : "orange";
                case LineVisitStatus.TestNotCovered:
                    return lightcolor ? "darkblack" : "yellow";
                case LineVisitStatus.newline:
                    return lightcolor ? "lightblue" : "blue";
                default:
                    return lightcolor ? "lightgray" : "gray";
            }
        }

        /// <summary>
        /// Gets the file name of the report file for the given class.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="className">Name of the class.</param>
        /// <returns>The file name.</returns>
        private static string GetClassReportFilename(string assemblyName, string className)
        {
            string key = assemblyName + "_" + className;

            string fileName = null;

            if (!FileNameByClass.TryGetValue(key, out fileName))
            {
                lock (FileNameByClass)
                {
                    if (!FileNameByClass.TryGetValue(key, out fileName))
                    {
                        string shortClassName = className.Substring(className.LastIndexOf('.') + 1);
                        fileName = RendererBase.ReplaceInvalidPathChars(assemblyName + "_" + shortClassName) + ".htm";

                        if (FileNameByClass.Values.Any(v => v.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                        {
                            int counter = 2;

                            do
                            {
                                fileName = RendererBase.ReplaceInvalidPathChars(assemblyName + "_" + shortClassName + counter) + ".htm";
                                counter++;
                            }
                            while (FileNameByClass.Values.Any(v => v.Equals(fileName, StringComparison.OrdinalIgnoreCase)));
                        }

                        FileNameByClass.Add(key, fileName);
                    }
                }
            }

            return fileName;
        }

        /// <summary>
        /// Saves the CSS.
        /// </summary>
        /// <param name="targetDirectory">The target directory.</param>
        private void SaveCss(string targetDirectory)
        {
            string targetPath = Path.Combine(targetDirectory, "report.css");

            using (var fs = new FileStream(targetPath, FileMode.Create))
            {
                using (var cssStream = this.GetCombinedCss())
                {
                    cssStream.CopyTo(fs);

                    if (!this.inlineCssAndJavaScript)
                    {
                        cssStream.Position = 0;
                        string css = new StreamReader(cssStream).ReadToEnd();

                        var matches = Regex.Matches(css, @"url\(pic_(?<filename>.+).png\),\surl\(data:image/png;base64,(?<base64image>.+)\)");

                        foreach (Match match in matches)
                        {
                            System.IO.File.WriteAllBytes(
                                Path.Combine(targetDirectory, "pic_" + match.Groups["filename"].Value + ".png"),
                                Convert.FromBase64String(match.Groups["base64image"].Value));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves the java script.
        /// </summary>
        /// <param name="targetDirectory">The target directory.</param>
        private void SaveJavaScript(string targetDirectory)
        {
            string targetPath = Path.Combine(targetDirectory, "combined.js");

            using (var fs = new FileStream(targetPath, FileMode.Create))
            {
                using (var javaScriptStream = this.GetCombinedJavascript())
                {
                    javaScriptStream.CopyTo(fs);
                }
            }
        }

        /// <summary>
        /// Gets the combined CSS.
        /// </summary>
        /// <returns>The combined CSS.</returns>
        private Stream GetCombinedCss()
        {
            var ms = new MemoryStream();

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
                "Palmmedia.ReportGenerator.Reporting.Rendering.resources.custom.css"))
            {
                stream.CopyTo(ms);
            }

            byte[] lineBreak = Encoding.UTF8.GetBytes(Environment.NewLine);
            ms.Write(lineBreak, 0, lineBreak.Length);
            ms.Write(lineBreak, 0, lineBreak.Length);

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
                "Palmmedia.ReportGenerator.Reporting.Rendering.resources.chartist.min.css"))
            {
                stream.CopyTo(ms);
            }

            ms.Position = 0;

            return ms;
        }

        /// <summary>
        /// Gets the combined javascript.
        /// </summary>
        /// <returns>The combined javascript.</returns>
        private Stream GetCombinedJavascript()
        {
            var ms = new MemoryStream();

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
                "Palmmedia.ReportGenerator.Reporting.Rendering.resources.jquery-1.11.2.min.js"))
            {
                stream.CopyTo(ms);
            }

            byte[] lineBreak = Encoding.UTF8.GetBytes(Environment.NewLine);
            ms.Write(lineBreak, 0, lineBreak.Length);

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
                "Palmmedia.ReportGenerator.Reporting.Rendering.resources.angular.min.js"))
            {
                stream.CopyTo(ms);
            }

            ms.Write(lineBreak, 0, lineBreak.Length);

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
                "Palmmedia.ReportGenerator.Reporting.Rendering.resources.react.modified.min.js"))
            {
                stream.CopyTo(ms);
            }

            ms.Write(lineBreak, 0, lineBreak.Length);

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
    "Palmmedia.ReportGenerator.Reporting.Rendering.resources.chartist.min.js"))
            {
                stream.CopyTo(ms);
            }

            ms.Write(lineBreak, 0, lineBreak.Length);

            // Required for rendering charts in IE 9
            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
    "Palmmedia.ReportGenerator.Reporting.Rendering.resources.matchMedia.js"))
            {
                stream.CopyTo(ms);
            }

            ms.Write(lineBreak, 0, lineBreak.Length);
            ms.Write(lineBreak, 0, lineBreak.Length);

            byte[] assembliesText = Encoding.UTF8.GetBytes(this.javaScriptContent.ToString());
            ms.Write(assembliesText, 0, assembliesText.Length);

            ms.Write(lineBreak, 0, lineBreak.Length);

            string MongoAPI = string.Empty;
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location.ToString().Replace("ReportGenerator.Reporting.dll", "ReportGenerator.exe");
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(path);
            if (configuration.HasFile)
            {
                AppSettingsSection appSettings = configuration.AppSettings;
                KeyValueConfigurationElement element = appSettings.Settings["MongoRestAPI"];
                MongoAPI = element.Value;
            }
            var sb = new StringBuilder();
            //    sb.Append(@"var mydata = [];
            //var MongoAPI = """ + MongoAPI + @""";
            //  var xhr = new XMLHttpRequest();
            //    xhr.open(""GET"", MongoAPI, false);
            //    xhr.send();
            //    data = JSON.parse(xhr.response);
            //    mydata = [];
            // for (var x in data.rows)
            //    {
            //        mydata.push(data.rows[x]);
            //    }
            //    ");
            sb.AppendLine("var buildcheck = true;");
            sb.AppendLine("var translations = {");
            sb.AppendFormat("'lineCoverage': '{0}'", CoverageType.LineCoverage.ToString());
            sb.AppendLine(",");
            sb.AppendFormat("'noGrouping': '{0}'", WebUtility.HtmlEncode(ReportResources.NoGrouping));
            sb.AppendLine(",");
            sb.AppendFormat("'byAssembly': '{0}'", WebUtility.HtmlEncode(ReportResources.ByAssembly));
            sb.AppendLine(",");
            sb.AppendFormat("'byNamespace': '{0}'", WebUtility.HtmlEncode(ReportResources.ByNamespace));
            sb.AppendLine(",");
            sb.AppendFormat("'all': '{0}'", WebUtility.HtmlEncode(ReportResources.All));
            sb.AppendLine(",");
            sb.AppendFormat("'collapseAll': '{0}'", WebUtility.HtmlEncode(ReportResources.CollapseAll));
            sb.AppendLine(",");
            sb.AppendFormat("'expandAll': '{0}'", WebUtility.HtmlEncode(ReportResources.ExpandAll));
            sb.AppendLine(",");
            sb.AppendFormat("'buildreport': '{0}'", WebUtility.HtmlEncode(ReportResources.buildhelper));
            sb.AppendLine(",");
            sb.AppendFormat("'allreport': '{0}'", WebUtility.HtmlEncode(ReportResources.allreport));
            sb.AppendLine(",");
            sb.AppendFormat("'asmbly': '{0}'", WebUtility.HtmlEncode(ReportResources.asmbly));
            sb.AppendLine(",");
            sb.AppendFormat("'Prev': '{0}'", "Prev");
            sb.AppendLine(",");
            sb.AppendFormat("'Next': '{0}'", "Next");
            sb.AppendLine(",");
            sb.AppendFormat("'grouping': '{0}'", WebUtility.HtmlEncode(ReportResources.Grouping));
            sb.AppendLine(",");
            sb.AppendFormat("'filter': '{0}'", WebUtility.HtmlEncode(ReportResources.Filter));
            sb.AppendLine(",");
            sb.AppendFormat("'name': '{0}'", WebUtility.HtmlEncode(ReportResources.Name));
            sb.AppendLine(",");
            sb.AppendFormat("'covered': '{0}'", WebUtility.HtmlEncode(ReportResources.Covered));
            sb.AppendLine(",");
            sb.AppendFormat("'uncovered': '{0}'", WebUtility.HtmlEncode(ReportResources.Uncovered));
            sb.AppendLine(",");
            sb.AppendFormat("'coverable': '{0}'", WebUtility.HtmlEncode(ReportResources.Coverable));
            sb.AppendLine(",");
            sb.AppendFormat("'total': '{0}'", WebUtility.HtmlEncode(ReportResources.Total));
            sb.AppendLine(",");

            sb.AppendFormat("'newlines': '{0}'", WebUtility.HtmlEncode(ReportResources.NewLines));
            sb.AppendLine(",");
            sb.AppendFormat("'testednewlines': '{0}'", WebUtility.HtmlEncode(ReportResources.TestedNewLines));
            sb.AppendLine(",");
            sb.AppendFormat("'newcoverage': '{0}'", WebUtility.HtmlEncode(ReportResources.NewCoverage));
            sb.AppendLine(",");
            sb.AppendFormat("'testcoverage': '{0}'", WebUtility.HtmlEncode(ReportResources.TestCoverage));
            sb.AppendLine(",");

            sb.AppendFormat("'coverage': '{0}'", WebUtility.HtmlEncode(ReportResources.Coverage));
            sb.AppendLine(",");
            sb.AppendFormat("'branchCoverage': '{0}'", WebUtility.HtmlEncode(ReportResources.BranchCoverage));
            sb.AppendLine(",");
            sb.AppendFormat("'history': '{0}'", WebUtility.HtmlEncode(ReportResources.History));
            sb.AppendLine();
            sb.AppendLine("};");

            byte[] translations = Encoding.UTF8.GetBytes(sb.ToString());
            ms.Write(translations, 0, translations.Length);

            ms.Write(lineBreak, 0, lineBreak.Length);
            ms.Write(lineBreak, 0, lineBreak.Length);

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
                "Palmmedia.ReportGenerator.Reporting.Rendering.resources.customReactComponents.js"))
            {
                stream.CopyTo(ms);
            }

            ms.Write(lineBreak, 0, lineBreak.Length);
            ms.Write(lineBreak, 0, lineBreak.Length);

            using (Stream stream = typeof(HtmlRenderer).Assembly.GetManifestResourceStream(
                "Palmmedia.ReportGenerator.Reporting.Rendering.resources.customAngularApp.js"))
            {
                stream.CopyTo(ms);
            }

            ms.Position = 0;

            return ms;
        }

        /// <summary>
        /// Initializes the text writer.
        /// </summary>
        /// <param name="targetPath">The target path.</param>
        private void CreateTextWriter(string targetPath)
        {
            this.reportTextWriter = new StreamWriter(new FileStream(targetPath, FileMode.Create));
        }

        /// <summary>
        /// Saves the report.
        /// </summary>
        private void SaveReport()
        {
            this.FinishReport();

            this.reportTextWriter.Flush();
            this.reportTextWriter.Dispose();

            this.reportTextWriter = null;
        }

        /// <summary>
        /// Finishes the report.
        /// </summary>
        private void FinishReport()
        {
            string javascript = "<script type=\"text/javascript\" src=\"combined.js\"></script>";
            javascript = javascript + scriptinsert();
            if (this.inlineCssAndJavaScript)
            {
                using (var javaScriptStream = this.GetCombinedJavascript())
                {
                    javascript = "<script type=\"text/javascript\">/* <![CDATA[ */ " + new StreamReader(javaScriptStream).ReadToEnd() + " /* ]]> */ </script>" + scriptinsert();
                }
            }

            this.reportTextWriter.Write(HtmlEnd, javascript);
        }
        public string scriptinsert()
        {


            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js\">  </script> ");


            sb.AppendLine(@" <script>
              $(document).ready(function() {
                   $('#body').show();
                   $('#msg').hide();
            });


        $(function () {
        
        var t2 = true;
        $(""#wrapper"").hide();
        $(""#m"").hide();
        $(document).on('click', '#img', function () {
            if (t2) {
                $(""#wrapper"").fadeIn(""slow"");
                $(""#img"").attr('src', 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABwAAAAcCAYAAAByDd+UAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAJiSURBVEhLrZZLaxNRGIYzyUyutpKEiJdgoGRRQVzanbNxK3TR/ADBRUtFBBeCl6alLbqSUhdCty3+hf6C0h9QXHQh9IKgKLhRKt7f93jOMPnmRKaZ88HT9nyX983JTM9MToeXgjRhm5PkvDAM/WGwgdHr9QpBEFwtFAoz+Xz+HnjI31j3kL8W75UacVi3Gvb7/TyL1Wr1PISfep63B/78hzdgsVKpXOQc5226rCUMmWTAaBYiH2KiafiIuTktkdgtcwOGqiuXCzC4JYROyyZ1KCb1I0MuEDTbFsOjsk09iiYMeVOoBT6ZGMrKa+pSf8CQf+C7v2MZyAx056kfGZJarXYOxc+y2RFfy+XyZe2lf3jeimhyzUv6qOh0OmUkDkSDaz7V6/WzytD3/SlLg3NwKt1UhkNull9gA+VZXvS0sB9zr8BvrROB+n1j+EQWwXtVHDEwfyz0yIopLooCeddqtc6wzts5LezvdrslzL8VemSVde7wgaWYdYeJmxA+j1SRjxhZBD/BEsq3UJ9OC/sxx2/sh9aJgOFtZVgsFq8gkbjIrsF/w3VlyHMOCT7PrI2OOGq32xX6qZMG212wNLnkBX0YypBPaiS/iCZXfC+VShPKDRF/Wjy2NLtgmfrGRxnqdxgfxR3RnJVd6AbmHScyNAu8NF1A074YGpV96lE37hEZmgSC1zPrTnegc4liUn/A0CQRfLdZBScxkTR8AzzCEi9QRjthaAoMHAqTEFgDh1pwGDys13mI6FHrGcy81dCgJhHNZnMMJ8UN3MnzEH4G+CGeY30X+bDRaIzrVqvRP0L/L9P7NabTQZpWAAAAAElFTkSuQmCC');
                t2 = false;
            }
            else if (!t2) {
                 $(""#wrapper"").hide();
                $(""#img"").attr('src', 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAB4AAAAeCAYAAAA7MK6iAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAKKSURBVEhLrZdPa9RQFMWTTMaZYcAWF25cKOjOQhezsNJiUBAEKR0X6W66aa1rXVfiStFvUJGC4h/QUpRCoStdufQjCCIIIoJLQWw855EX0js3L2bSC79m8s6752QybyavXla+g2KdAJfAOkjAfXAXrIIFMAWKpflZPD+KolASx3GLIqvVai36vr8NfoDUwXfwCvOvZa2e5k2ojQWbDhQMrsDoY8G4Du/DMJynT5qmasahQQ6w0PhQGE1EEAT8SEzJnDzYqJ53DA1vpUFDXsA3oPlYcJIkFDDHf1doOEoY7jEnD7YLCeIjMdnFU7TM4LgjxkvBbd9gTp7HP1gIkTa5DLtycYw1vYSDdrs9YJ+tAIOfxCQnCByaxiAYabqDDyaRBZPrygQn6LnBXgSvaLoL3F3+CJnPtvYqbhIMuD68abz4JYRKGgZ/Y2+tRWVpGJyy95YmVIHgOAte1fQq2HtPE8AzaEsIGPLdSXq93il+HzudzhlNB1z1Q/i8Eb4GBj9QhIN+v3+SoqsGg0E7e1lauLCzir8JTjQB7EAb4VaSFUm32z2dveNzmg5G7IfPnvA1MPimJlQB42U247im6VWwd0ETquDnmAVPvKqP48VPKVTRMPgre/nL9VoIlTQMfmKCYXJVEZ00CcZv9ZwJZmGg1t4KwZM+nfZNIMo+jy8ok0pBsHkeI3hZ00v4g+fxefax8h0ITMq+0xrbaLmIo/o91YD/beZoey7e8pey4Yh4TH++yUPB9gTF3chz0dSUTRoX99c8z08K4bztG2j4Kwzq8hs+dzLL8n21hVdGIVtw+8Lsf9nFQpqlj93SFuH4WLCFIgsr+DKMtsCXgrHGZ7CJCzb/urA03yiKwn902r1+I8rzxwAAAABJRU5ErkJggg==');
                t2 = true;
            }
        });
    }); 
 </script>");




            return sb.ToString();

        }
        /// <summary>
        /// Filters the historic coverages (equal elements are removed).
        /// </summary>
        /// <param name="historicCoverages">The historic coverages.</param>
        /// <param name="maximum">The maximum.</param>
        /// <returns>The filtered historic coverages.</returns>
        private IEnumerable<HistoricCoverage> FilterHistoricCoverages(IEnumerable<HistoricCoverage> historicCoverages, int maximum)
        {
            var result = new List<HistoricCoverage>();

            foreach (var historicCoverage in historicCoverages)
            {
                if (result.Count == 0 || !result[result.Count - 1].Equals(historicCoverage))
                {
                    result.Add(historicCoverage);
                }
            }

            result.RemoveRange(0, Math.Max(0, result.Count - maximum));

            return result;
        }
    }
}
