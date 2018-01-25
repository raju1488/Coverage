var i, l, numberOfClasses;
var assemblies = [];
var start = 0;
var st = 0;
var end = 0;
var stop = 0;
var assembliescount = 5;
var all = true;
/* Angular controller for summary report */
function SummaryViewCtrl($scope, $window) {
    var self = this;
    $scope.filteringEnabled = false;
    $scope.assemblies = [];
    $scope.branchCoverageAvailable = branchCoverageAvailable;
	
	$scope.asmblycount = codesum.asmblycount;
	$scope.classescount = codesum.classescount;
	$scope.files = codesum.files;
	$scope.coveredLines = codesum.coveredLines;
	$scope.uncoveredLines = codesum.uncoveredLines;
	$scope.coverablelines = codesum.coverablelines;
	$scope.linecoverage = codesum.linecoverage;
	$scope.totallines = codesum.linecoverage;
	$scope.modifiedasmblycount = codesum.modifiedasmblycount;
	$scope.buildlines = codesum.buildlines;
	$scope.buildtestedines = codesum.buildtestedines;
	$scope.buildcoverage = codesum.buildcoverage;

    $scope.enableFiltering = function () {
        console.log("Enabling filtering");

        $scope.assemblies = assemblies;
        $scope.filteringEnabled = true;
    };
    $scope.loadData = function () {
        if (start == 0) {
            st = 1;
            for (start = 0; start < assembliescount && start < mydata.length; start++) {
                assemblies.push(mydata[start]);
            }

            end = start;
            if (start > mydata.length) {
                end = start--;
            }
        }
    };
    self.initialize = function () {

        if ($window.history === undefined || $window.history.replaceState === undefined || $window.history.state === null) {
            numberOfClasses = 0;

            for (i = 0, l = assemblies.length; i < l; i++) {
                numberOfClasses += assemblies[i].classes.length;
                if (numberOfClasses > 1500) {
                    console.log("Number of classes (filtering disabled): " + numberOfClasses);
                    return;
                }
            }

            console.log("Number of classes (filtering enabled): " + numberOfClasses);
        }

        $scope.enableFiltering();
        $scope.loadData();
    };

    self.initialize();
}

/* Angular controller for class reports */
function DetailViewCtrl($scope, $window) {
    var self = this;

    $scope.selectedTestMethod = "AllTestMethods";

    $scope.switchTestMethod = function (method) {
        console.log("Selected test method: " + method);
        var lines, i, l, coverageData, lineAnalysis, cells;

        lines = document.querySelectorAll('.lineAnalysis tr');

        for (i = 1, l = lines.length; i < l; i++) {
            coverageData = JSON.parse(lines[i].getAttribute('data-coverage').replace(/'/g, '"'));
            lineAnalysis = coverageData[method];
            cells = lines[i].querySelectorAll('td');
            if (lineAnalysis === null) {
                lineAnalysis = coverageData.AllTestMethods;
                if (lineAnalysis.LVS !== 'gray') {
                    cells[0].setAttribute('class', 'red');
                    cells[1].innerText = cells[1].textContent = '0';
                    cells[4].setAttribute('class', 'lightred');
                }
            } else {
                cells[0].setAttribute('class', lineAnalysis.LVS);
                cells[1].innerText = cells[1].textContent = lineAnalysis.VC;
                cells[4].setAttribute('class', 'light' + lineAnalysis.LVS);
            }
        }
    };

    $scope.navigateToHash = function (hash) {
        // Prevent history entries when selecting methods/properties
        if ($window.history !== undefined && $window.history.replaceState !== undefined) {
            $window.history.replaceState(undefined, undefined, hash);
        }
    };
}

/* Angular application */
var coverageApp = angular.module('coverageApp', []);
coverageApp.controller('SummaryViewCtrl', SummaryViewCtrl);
coverageApp.controller('DetailViewCtrl', DetailViewCtrl);

coverageApp.directive('reactiveTable', function () {
    return {
        restrict: 'A',
        scope: {
            assemblies: '=',
            branchCoverageAvailable: '='
        },
        link: function (scope, el, attrs) {
            scope.$watchCollection('assemblies', function (newValue, oldValue) {
                React.renderComponent(
                    AssemblyComponent({ assemblies: newValue, branchCoverageAvailable: scope.branchCoverageAvailable }),
                    el[0]);
            });
        }
    };
});

coverageApp.directive('historyChart', function ($window) {
    return {
        restrict: 'A',
        link: function (scope, el, attrs) {
            var chartData = $window[attrs.data];
            new Chartist.Line('#' + el[0].id, {
                labels: [],
                series: chartData.series
            }, {
                lineSmooth: false,
                low: 0,
                high: 100
            });

            var chart = $(el[0]);

            var tooltip = chart
              .append('<div class="tooltip"></div>')
              .find('.tooltip');

            chart.on('mouseenter', '.ct-point', function () {
                var point = $(this);
                var index = point.parent().children('.ct-point').index(point);

                tooltip
                    .html(chartData.tooltips[index % chartData.tooltips.length])
                    .show();
            });

            chart.on('mouseleave', '.ct-point', function () {
                tooltip.hide();
            });

            chart.on('mousemove', function (event) {
                var box = el[0].getBoundingClientRect();
                var left = event.pageX - box.left - window.pageXOffset;
                var top = event.pageY - box.top - window.pageYOffset;

                tooltip.css({
                    left: left - tooltip.width() / 2 - 5,
                    top: top - tooltip.height() - 40
                });
            });

        }
    };
});