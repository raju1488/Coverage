using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Palmmedia.ReportGenerator.Parser.Analysis;
using Palmmedia.ReportGenerator.Properties;
using Palmmedia.ReportGenerator.Reporting.Rendering;

namespace Palmmedia.ReportGenerator.Reporting
{
    /// <summary>
    /// Implementation of <see cref="IReportBuilder"/> that uses <see cref="IReportRenderer"/> to create reports.
    /// </summary>
    public abstract class ReportBuilderBase : IReportBuilder
    {
        /// <summary>
        /// Gets the report type.
        /// </summary>
        /// <value>
        /// The report type.
        /// </value>
        public abstract string ReportType { get; }

        /// <summary>
        /// Gets or sets the target directory where reports are stored.
        /// </summary>
        /// <value>
        /// The target directory.
        /// </value>
        public string TargetDirectory { get; set; }

        /// <summary>
        /// Creates a class report.
        /// </summary>
        /// <param name="reportRenderer">The report renderer.</param>
        /// <param name="class">The class.</param>
        /// <param name="fileAnalyses">The file analyses that correspond to the class.</param>
        public virtual void CreateClassReport(IReportRenderer reportRenderer, Class @class, IEnumerable<FileAnalysis> fileAnalyses)
        {
            if (reportRenderer == null)
            {
                throw new ArgumentNullException(nameof(reportRenderer));
            }

            if (@class == null)
            {
                throw new ArgumentNullException(nameof(@class));
            }

            if (fileAnalyses == null)
            {
                throw new ArgumentNullException(nameof(fileAnalyses));
            }

            reportRenderer.BeginClassReport(this.TargetDirectory, @class.Assembly.ShortName, @class.Name);

            reportRenderer.Header(ReportResources.Summary);
            //int val;
            //if (@class.TestedNewLines != 0 && @class.NewLines != 0 && @class.CoverableLines!=0)
            //{
            //    val = (int)((@class.CoverableLines == 0) ? (decimal?)null : (decimal)Math.Truncate(100 * ((double)@class.TestedNewLines / (double)@class.NewLines)));
            //}
            //else
            //    val = 0;
            reportRenderer.BeginKeyValueTable();
            reportRenderer.KeyValueRow(ReportResources.Class, @class.Name);
            reportRenderer.KeyValueRow(ReportResources.Assembly, @class.Assembly.ShortName);
            reportRenderer.KeyValueRow(ReportResources.Files3, @class.Files.Select(f => f.Path));
            reportRenderer.KeyValueRow(ReportResources.CoveredLines, @class.CoveredLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.UncoveredLines, (@class.CoverableLines - @class.CoveredLines).ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.CoverableLines, @class.CoverableLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.NewLines, @class.NewLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.TestedNewLines, @class.TestedNewLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.TotalLines, @class.TotalLines.GetValueOrDefault().ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.Coverage2, @class.CoverageQuota.HasValue ? @class.CoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
            reportRenderer.KeyValueRow(ReportResources.NewCoverage, @class.NewCoverage != 0 ? @class.NewCoverage.ToString(CultureInfo.InvariantCulture) + "%" : "0%");

            decimal? branchCoverage = @class.BranchCoverageQuota;

            if (branchCoverage.HasValue)
            {
                reportRenderer.KeyValueRow(ReportResources.BranchCoverage2, branchCoverage.Value.ToString(CultureInfo.InvariantCulture) + "%");
            }

            reportRenderer.FinishTable();

            if (@class.HistoricCoverages.Any(h => h.CoverageQuota.HasValue || h.BranchCoverageQuota.HasValue))
            {
                reportRenderer.Header(ReportResources.History);
                reportRenderer.Chart(@class.HistoricCoverages);
            }

            var metrics = @class.MethodMetrics;

            if (metrics.Any())
            {
                reportRenderer.Header(ReportResources.Metrics);

                reportRenderer.BeginMetricsTable(metrics.First());

                foreach (var metric in metrics)
                {
                    reportRenderer.MetricsRow(metric);
                }

                reportRenderer.FinishTable();
            }

            reportRenderer.Header(ReportResources.Files);

            if (fileAnalyses.Any())
            {
                int fileIndex = 0;
                foreach (var fileAnalysis in fileAnalyses)
                {
                    reportRenderer.File(fileAnalysis.Path);

                    if (!string.IsNullOrEmpty(fileAnalysis.Error))
                    {
                        reportRenderer.Paragraph(fileAnalysis.Error);
                    }
                    else
                    {
                        reportRenderer.BeginLineAnalysisTable(new[] { string.Empty, "#", ReportResources.Line, string.Empty, ReportResources.Coverage });

                        foreach (var line in fileAnalysis.Lines)
                        {
                            //if (line.LineVisitStatus == LineVisitStatus.newline || line.LineVisitStatus == LineVisitStatus.TestNotCovered)
                                reportRenderer.LineAnalysis(fileIndex, line);
                        }

                        reportRenderer.FinishTable();
                    }

                    fileIndex++;
                }
            }
            else
            {
                reportRenderer.Paragraph(ReportResources.NoFilesFound);
            }

            reportRenderer.AddFooter();

            if (fileAnalyses.Any())
            {
                var testMethods = @class.Files
                    .SelectMany(f => f.TestMethods)
                    .Distinct()
                    .OrderBy(l => l.ShortName);

                var codeElementsByFileIndex = new Dictionary<int, IEnumerable<CodeElement>>();
                int fileIndex = 0;
                List<int> val = new List<int>();
                List<int> val1 = new List<int>();
                List<int> less = new List<int>();
                foreach (var file in @class.Files)
                {
                    fileIndex++;
                    val = new List<int>();
                    val = file.CodeElements.OrderBy(c => c.Line).Select(x => x.Line).ToList();
                    if (@class.ChangesCount.Count > 0)
                    {
                        for (int j = 0; j < @class.ChangesCount.Count; j++)
                        {
                            for (int i = 0; i < val.Count; i++)
                            {
                                string[] stringSeparators = new string[] { "[stop]" };
                                int value = Convert.ToInt32(@class.ChangesCount[j].Split(new[] { "###" }, StringSplitOptions.None) != null ? @class.ChangesCount[j].Split(new[] { "###" }, StringSplitOptions.None)[0] : "");

                                if (val[i] == value)
                                {
                                    val1.Add(val[i]);
                                }
                                if (val[i] < value)
                                {
                                    less.Add(val[i]);
                                }
                            }
                            if (less.Count > 0)
                            {
                                val1.Add(less.Max());

                            }
                            less = new List<int>();
                        }
                        val1 = val1.Distinct().ToList();
                        val1.Sort();
                    }
                    if (val1.Count > 0)
                        codeElementsByFileIndex.Add(fileIndex, file.CodeElements.OrderBy(c => c.Line).Where(x => val1.Contains(x.Line)));
                    else
                        codeElementsByFileIndex.Add(fileIndex, file.CodeElements.OrderBy(c => c.Line));
                }
                reportRenderer.TestMethods(testMethods, codeElementsByFileIndex);
            }
            reportRenderer.SaveClassReport(this.TargetDirectory, @class.Assembly.ShortName, @class.Name);
        }

        /// <summary>
        /// Creates the summary report.
        /// </summary>
        /// <param name="reportRenderer">The report renderer.</param>
        /// <param name="summaryResult">The summary result.</param>
        public virtual void CreateSummaryReport(IReportRenderer reportRenderer, SummaryResult summaryResult)
        {
            if (reportRenderer == null)
            {
                throw new ArgumentNullException(nameof(reportRenderer));
            }

            if (summaryResult == null)
            {
                throw new ArgumentNullException(nameof(summaryResult));
            }

            reportRenderer.BeginSummaryReport(this.TargetDirectory, null, ReportResources.Summary);
            //reportRenderer.Header(ReportResources.Summary);
            int val;
            if (summaryResult.TestedNewLines != 0 && summaryResult.NewLines != 0 && summaryResult.CoverableLines != 0)
            {
                val = (int)((summaryResult.CoverableLines == 0) ? (decimal?)null : (decimal)Math.Truncate(100 * ((double)summaryResult.TestedNewLines / (double)summaryResult.NewLines)));
            }
            else
                val = 0;
            string value = val != 0 ? val.ToString(CultureInfo.InvariantCulture) + "%" : "0%";
            reportRenderer.Begindiv(summaryResult.BuildVersion, value);

            reportRenderer.BeginKeyValueTable1();
            //reportRenderer.KeyValueRow(ReportResources.GeneratedOn, DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString());
            //reportRenderer.KeyValueRow(ReportResources.Parser, summaryResult.UsedParser);
            reportRenderer.KeyValueRow(ReportResources.Assemblies2, summaryResult.Assemblies.Count().ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.Classes, summaryResult.Assemblies.SelectMany(a => a.Classes).Count().ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.Files2, summaryResult.Assemblies.SelectMany(a => a.Classes).SelectMany(a => a.Files).Distinct().Count().ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.CoveredLines, summaryResult.CoveredLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.UncoveredLines, (summaryResult.CoverableLines - summaryResult.CoveredLines).ToString(CultureInfo.InvariantCulture));
            //reportRenderer.KeyValueRow(ReportResources.NewLines, summaryResult.NewLines.ToString(CultureInfo.InvariantCulture));
            //reportRenderer.KeyValueRow(ReportResources.TestedNewLines, summaryResult.TestedNewLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.TotalLines, summaryResult.TotalLines.GetValueOrDefault().ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.Coverage2, summaryResult.CoverageQuota.HasValue ? summaryResult.CoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
            //reportRenderer.KeyValueRow(ReportResources.TestCoverage, val != 0 ? val.ToString(CultureInfo.InvariantCulture) + "%" : "0%");
            //reportRenderer.KeyValueRow(ReportResources.TestCoverage, summaryResult.CoverageQuota.HasValue ? summaryResult.CoverageQuota.Value.ToString(CultureInfo.InvariantCulture) + "%" : string.Empty);
            decimal? branchCoverage = summaryResult.BranchCoverageQuota;

            if (branchCoverage.HasValue)
            {
                reportRenderer.KeyValueRow(ReportResources.BranchCoverage2, branchCoverage.Value.ToString(CultureInfo.InvariantCulture) + "%");
            }

            reportRenderer.FinishTable();

            reportRenderer.BeginKeyValueTable2();
            reportRenderer.KeyValueRow(ReportResources.ModifiedAssemblies, summaryResult.modifyassemblies.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.NewLines, summaryResult.NewLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.TestedNewLines, summaryResult.TestedNewLines.ToString(CultureInfo.InvariantCulture));
            reportRenderer.KeyValueRow(ReportResources.TestCoverage, val != 0 ? val.ToString(CultureInfo.InvariantCulture) + "%" : "0%");

            reportRenderer.FinishTable();

            reportRenderer.Enddiv();

            var historicCoverages = this.GetOverallHistoricCoverages(summaryResult.Assemblies.SelectMany(a => a.Classes));
            if (historicCoverages.Any(h => h.CoverageQuota.HasValue || h.BranchCoverageQuota.HasValue))
            {
                reportRenderer.Header(ReportResources.History);
                reportRenderer.Chart(historicCoverages);
            }

            reportRenderer.Header(ReportResources.Assemblies);
            System.Diagnostics.Debugger.Launch();
            if (summaryResult.Assemblies.Any())
            {
                reportRenderer.BeginSummaryTable(summaryResult.SupportsBranchCoverage);

                //foreach (var assembly in summaryResult.Assemblies)
                //{
                //    reportRenderer.SummaryAssembly(assembly, summaryResult.SupportsBranchCoverage);

                //    foreach (var @class in assembly.Classes)
                //    {
                //        reportRenderer.SummaryClass(@class, summaryResult.SupportsBranchCoverage);
                //    }
                //}

                //reportRenderer.FinishTable();
            }
            else
            {
                reportRenderer.Paragraph(ReportResources.NoCoveredAssemblies);
            }

            reportRenderer.CustomSummary(summaryResult.Assemblies, summaryResult.SupportsBranchCoverage);
            reportRenderer.AddFooter();
            reportRenderer.SaveSummaryReport(this.TargetDirectory);
        }

        /// <summary>
        /// Creates a class report.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <param name="fileAnalyses">The file analyses that correspond to the class.</param>
        public abstract void CreateClassReport(Class @class, IEnumerable<FileAnalysis> fileAnalyses);

        /// <summary>
        /// Creates the summary report.
        /// </summary>
        /// <param name="summaryResult">The summary result.</param>
        public abstract void CreateSummaryReport(SummaryResult summaryResult);

        /// <summary>
        /// Gets the overall historic coverages from all classes.
        /// </summary>
        /// <param name="classes">The classes.</param>
        /// <returns>
        /// The overall historic coverages from all classes.
        /// </returns>
        private IEnumerable<HistoricCoverage> GetOverallHistoricCoverages(IEnumerable<Class> classes)
        {
            var historicCoverages = classes
                .SelectMany(c => c.HistoricCoverages);

            var executionTimes = historicCoverages
                .Select(h => h.ExecutionTime)
                .Distinct()
                .OrderBy(e => e);

            var result = new List<HistoricCoverage>();

            foreach (var executionTime in executionTimes)
            {
                var historicCoveragesOfExecutionTime = historicCoverages
                    .Where(h => h.ExecutionTime.Equals(executionTime))
                    .ToArray();

                result.Add(new HistoricCoverage(executionTime)
                {
                    CoveredLines = historicCoveragesOfExecutionTime.Sum(h => h.CoveredLines),
                    CoverableLines = historicCoveragesOfExecutionTime.Sum(h => h.CoverableLines),
                    CoveredBranches = historicCoveragesOfExecutionTime.Sum(h => h.CoveredBranches),
                    TotalBranches = historicCoveragesOfExecutionTime.Sum(h => h.TotalBranches),
                    TotalLines = historicCoveragesOfExecutionTime.Sum(h => h.TotalLines)
                });
            }

            return result;
        }
    }
}
