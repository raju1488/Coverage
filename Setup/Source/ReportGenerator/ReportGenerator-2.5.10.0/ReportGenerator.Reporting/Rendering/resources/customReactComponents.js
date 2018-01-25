/* Helper methods */
function createRandomId(length) {
    var possible = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789', id = '', i;

    for (i = 0; i < length; i++) {
        id += possible.charAt(Math.floor(Math.random() * possible.length));
    }

    return id;
}

function roundNumber(number, precision) {
    return Math.floor(number * Math.pow(10, precision)) / Math.pow(10, precision);
}

function getNthOrLastIndexOf(text, substring, n) {
    var times = 0, index = -1, currentIndex = -1;

    while (times < n) {
        currentIndex = text.indexOf(substring, index + 1);
        if (currentIndex === -1) {
            break;
        } else {
            index = currentIndex;
        }

        times++;
    }

    return index;
}

/* Data models */
function ClassViewModel(serializedClass) {
    var self = this;
    self.isNamespace = false;
    self.name = serializedClass.name;
    self.parent = null;
    self.reportPath = serializedClass.reportPath;
    self.coveredLines = serializedClass.coveredLines;
    self.uncoveredLines = serializedClass.uncoveredLines;
    self.coverableLines = serializedClass.coverableLines;
    self.newlines = serializedClass.newlines;
    self.testednewlines = serializedClass.testednewlines;

    self.totalLines = serializedClass.totalLines;
    self.coverageType = serializedClass.coverageType;
    self.coveredBranches = serializedClass.coveredBranches,
    self.totalBranches = serializedClass.totalBranches;
    self.lineCoverageHistory = serializedClass.lineCoverageHistory;
    self.branchCoverageHistory = serializedClass.branchCoverageHistory;
    self.testcoveragePercent = '';
    self.testcoverageTitle = '';

    if (self.newlines != 0 && self.testednewlines != 0) {
        self.testcoverage = roundNumber((100 * serializedClass.testednewlines) / serializedClass.newlines, 1);
        self.testcoveragePercent = self.testcoverage + '%';
        self.testcoverageTitle = serializedClass.coverageType;
    }
    else {
        self.testcoverage = serializedClass.testcoverage;
        self.testcoveragePercent = self.testcoverage;
        self.testcoverageTitle = serializedClass.coverageType;
    }
    if (serializedClass.coverableLines === 0) {
        if (isNaN(serializedClass.methodCoverage)) {
            self.coverage = NaN;
            self.coveragePercent = '';
            self.coverageTitle = '';
        } else {
            self.coverage = serializedClass.methodCoverage;
            self.coveragePercent = self.coverage + '%';
            self.coverageTitle = serializedClass.coverageType;
        }
    } else {
        self.coverage = roundNumber((100 * serializedClass.coveredLines) / serializedClass.coverableLines, 1);
        self.coveragePercent = self.coverage + '%';
        self.coverageTitle = serializedClass.coverageType;
    }
    if (serializedClass.totalBranches === 0) {
        self.branchCoverage = NaN;
        self.branchCoveragePercent = '';
    } else {
        self.branchCoverage = roundNumber((100 * serializedClass.coveredBranches) / serializedClass.totalBranches, 1);
        self.branchCoveragePercent = self.branchCoverage + '%';
    }

    self.visible = function (filter) {
        return filter === '' || self.name.toLowerCase().indexOf(filter) > -1;
    };
}

function CodeElementViewModel(DataItem, parent) {
    var self = this;
    self.isNamespace = true;
    self.name = DataItem.name;
    self.parent = parent;
    self.subelements = [];
    self.coverageType = translations.lineCoverage;
    self.collapsed = name.indexOf('Test') > -1 && parent === null;
	
	self.cnxt = false;
	self.cprv = false;
    self.coveredLines = DataItem.coveredLines;
    self.uncoveredLines = DataItem.uncoveredLines;
    self.coverableLines = DataItem.coverableLines;
    self.newlines = DataItem.newlines;
    self.testednewlines = DataItem.testednewlines;

    self.totalLines = DataItem.totalLines;

    self.coveredBranches = DataItem.coveredBranches;
    self.totalBranches = DataItem.totalBranches;
	
    self.testcoverage = (DataItem.testcoverage !=null)? DataItem.testcoverage : 0;
	self.coverage = (DataItem.coverage != null)? DataItem.coverage : 0;
	self.branchcoverage = (DataItem.branchcoverage !=null)? DataItem.branchcoverage : 0;


    // self.testcoverage = function () {
        // if (self.testednewlines == 0 && self.newlines == 0) {
            // return 0;
        // }

        // return roundNumber(100 * self.testednewlines / self.newlines, 1);
    // };

    // self.coverage = function () {
        // if (self.coverableLines === 0) {
            // return NaN;
        // }

        // return roundNumber(100 * self.coveredLines / self.coverableLines, 1);
    // };

    // self.branchCoverage = function () {
        // if (self.totalBranches === 0) {
            // return NaN;
        // }

        // return roundNumber(100 * self.coveredBranches / self.totalBranches, 1);
    // };

    self.visible = function (filter) {
        var i, l;
        // for (i = 0, l = self.subelements.length; i < l; i++) {
            // if (self.subelements[i].visible(filter)) {
                // return true;
            // }
        // }

        return filter === '' || self.name.toLowerCase().indexOf(filter) > -1;
    };
	// self.visible = function (filter) {
        // return filter === '' || self.name.toLowerCase().indexOf(filter) > -1;
    // };

    self.build1 = function () {
        var i, l;
        for (i = 0, l = self.subelements.length; i < l; i++) {
            if (self.subelements[i].newlines > 0) {
                return true;
            }
        }
        return false;
    };

    self.insertClass = function (clazz, grouping) {
        var groupingDotIndex, groupedNamespace, i, l, subNamespace;

        self.coveredLines += clazz.coveredLines;
        self.uncoveredLines += clazz.uncoveredLines;
        self.coverableLines += clazz.coverableLines;
        self.newlines += clazz.newlines;
        self.testednewlines += clazz.testednewlines;
        self.totalLines += clazz.totalLines;

        self.coveredBranches += clazz.coveredBranches;	
        self.totalBranches += clazz.totalBranches;

        if (all) {
            if (grouping === undefined) {
                clazz.parent = self;
                self.subelements.push(clazz);
                return;
            }

            groupingDotIndex = getNthOrLastIndexOf(clazz.name, '.', grouping);
            groupedNamespace = groupingDotIndex === -1 ? '-' : clazz.name.substr(0, groupingDotIndex);

            for (i = 0, l = self.subelements.length; i < l; i++) {
                if (self.subelements[i].name === groupedNamespace) {
                    self.subelements[i].insertClass(clazz);
                    return;
                }
            }

            subNamespace = new CodeElementViewModel(groupedNamespace, self);
            self.subelements.push(subNamespace);
            subNamespace.insertClass(clazz);
        }
    };

    self.collapse = function () {
       var i, l, element;
       self.collapsed = false;
    };
	self.cnxt = function () {
       var i, l, element;
       self.cnxt = true;
    };
	self.cprv = function () {
       var i, l, element;
       self.cprv = true;
    };
    self.build = function () {
        var i, l, element;
        for (i = 0, l = self.subelements.length; i < l; i++) {
            if (self.subelements[i].newlines > 0) {
                element = self.subelements[i];

                if (element.isNamespace) {
                    element.build();
                }
            }
        }
    };

    self.toggleCollapse = function () {
        self.collapsed = !self.collapsed;
    };
	
	self.NextCollapse = function () {
		self.cnxt = true;
		self.cprv = false;
		ctmp = true;
        //self.cnxt = !self.cnxt;
    };
	self.PrevCollapse = function () {
		self.cnxt = false;
        self.cprv = true;
		ctmp = false;
    };

    self.changeSorting = function (sortby, ascending) {
        var smaller = ascending ? -1 : 1, bigger = ascending ? 1 : -1, i, l, element;

        if (sortby === 'name') {
            self.subelements.sort(function (left, right) {
                return left.name === right.name ? 0 : (left.name < right.name ? smaller : bigger);
            });
        } else {
            if (self.subelements.length > 0 && self.subelements[0].isNamespace) {
                // Top level elements are resorted ASC by name if other sort columns than 'name' is selected
                self.subelements.sort(function (left, right) {
                    return left.name === right.name ? 0 : (left.name < right.name ? -1 : 1);
                });
            } else {
                if (sortby === 'covered') {
                    self.subelements.sort(function (left, right) {
                        return left.coveredLines === right.coveredLines ?
                                0
                                : (left.coveredLines < right.coveredLines ? smaller : bigger);
                    });
                } else if (sortby === 'uncovered') {
                    self.subelements.sort(function (left, right) {
                        return left.uncoveredLines === right.uncoveredLines ?
                                0
                                : (left.uncoveredLines < right.uncoveredLines ? smaller : bigger);
                    });
                } else if (sortby === 'coverable') {
                    self.subelements.sort(function (left, right) {
                        return left.coverableLines === right.coverableLines ?
                                0
                                : (left.coverableLines < right.coverableLines ? smaller : bigger);
                    });
                } else if (sortby === 'newlines') {
                    self.subelements.sort(function (left, right) {
                        return left.newlines === right.newlines ?
                                0
                                : (left.newlines < right.newlines ? smaller : bigger);
                    });
                } else if (sortby === 'testednewlines') {
                    self.subelements.sort(function (left, right) {
                        return left.testednewlines === right.testednewlines ?
                                0
                                : (left.testednewlines < right.testednewlines ? smaller : bigger);
                    });
                } else if (sortby === 'testcoverage') {
                    self.subelements.sort(function (left, right) {
                        if (left.testcoverage === right.testcoverage) {
                            return 0;
                        } else if (isNaN(left.testcoverage)) {
                            return smaller;
                        } else if (isNaN(right.testcoverage)) {
                            return bigger;
                        } else {
                            return left.testcoverage < right.testcoverage ? smaller : bigger;
                        }
                    });
                } else if (sortby === 'total') {
                    self.subelements.sort(function (left, right) {
                        return left.totalLines === right.totalLines ?
                                0
                                : (left.totalLines < right.totalLines ? smaller : bigger);
                    });
                } else if (sortby === 'coverage') {
                    self.subelements.sort(function (left, right) {
                        if (left.coverage === right.coverage) {
                            return 0;
                        } else if (isNaN(left.coverage)) {
                            return smaller;
                        } else if (isNaN(right.coverage)) {
                            return bigger;
                        } else {
                            return left.coverage < right.coverage ? smaller : bigger;
                        }
                    });
                } else if (sortby === 'branchcoverage') {
                    self.subelements.sort(function (left, right) {
                        if (left.branchCoverage === right.branchCoverage) {
                            return 0;
                        } else if (isNaN(left.branchCoverage)) {
                            return smaller;
                        } else if (isNaN(right.branchCoverage)) {
                            return bigger;
                        } else {
                            return left.branchCoverage < right.branchCoverage ? smaller : bigger;
                        }
                    });
                }
            }
        }

        for (i = 0, l = self.subelements.length; i < l; i++) {
            element = self.subelements[i];

            if (element.isNamespace) {
                element.changeSorting(sortby, ascending);
            }
        }
    };
}

/* React components */
var AssemblyComponent = React.createClass({
    getAssemblies: function (assemblies, grouping, sortby, sortorder) {
        var i, l, j, l2, assemblyElement, parentElement, cls, smaller, bigger, result;

        result = [];

        if (grouping === '0') { // Group by assembly
            for (i = 0, l = assemblies.length; i < l; i++) {
                assemblyElement = new CodeElementViewModel(assemblies[i], null);
                result.push(assemblyElement);

                // for (j = 0, l2 = assemblies[i].classes.length; j < l2; j++) {
                    // cls = assemblies[i].classes[j];
                    // assemblyElement.insertClass(new ClassViewModel(cls));
                // }
            }
        // } else if (grouping === '-1') { // no grouping
            // parentelement = new codeelementviewmodel(translations.all, null);
            // result.push(parentelement);

            // // for (i = 0, l = assemblies.length; i < l; i++) {
                // // for (j = 0, l2 = assemblies[i].classes.length; j < l2; j++) {
                    // // cls = assemblies[i].classes[j];
                    // // parentelement.insertclass(new classviewmodel(cls));
                // // }
            // // }
        // } else { // group by assembly and namespace
            // for (i = 0, l = assemblies.length; i < l; i++) {
                // assemblyelement = new codeelementviewmodel(assemblies[i], null);
                // result.push(assemblyelement);

                // // for (j = 0, l2 = assemblies[i].classes.length; j < l2; j++) {
                    // // cls = assemblies[i].classes[j];
                    // // assemblyelement.insertclass(new classviewmodel(cls), grouping);
                // // }
            // }
        }

        if (sortby === 'name') {
            smaller = sortorder === 'asc' ? -1 : 1;
            bigger = sortorder === 'asc' ? 1 : -1;
        } else {
            smaller = -1;
            bigger = 1;
        }

        result.sort(function (left, right) {
            return left.name === right.name ? 0 : (left.name < right.name ? smaller : bigger);
        });

        for (i = 0, l = result.length; i < l; i++) {
            result[i].changeSorting(sortby, sortorder === 'asc');
        }

        return result;
    },
    getGroupingMaximum: function (assemblies) {
        var i, l, j, l2, result;

        result = 1;

        // for (i = 0, l = assemblies.length; i < l; i++) {
            // for (j = 0, l2 = assemblies[i].classes.length; j < l2; j++) {
                // result = Math.max(
                    // result,
                    // (assemblies[i].classes[j].name.match(/\./g) || []).length
                // );
            // }
        // }

        console.log("Grouping maximum: " + result);

        return result;
    },
    getInitialState: function () {
        var state, collapseState;

        if (window.history !== undefined && window.history.replaceState !== undefined && window.history.state !== null) {
            state = angular.copy(window.history.state);
            collapseState = state.assemblies;
        } else {
            state = {
                grouping: '0',
                groupingMaximum: this.getGroupingMaximum(this.props.assemblies),
                filter: '',
                sortby: 'name',
                sortorder: 'asc',
                assemblies: null,
                branchCoverageAvailable: this.props.branchCoverageAvailable
            };
        }
        state.assemblies = this.getAssemblies(this.props.assemblies[0], state.grouping, state.filter, state.sortby, state.sortorder);

        if (collapseState !== undefined) {
            this.restoreCollapseState(collapseState, state.assemblies);
        }

        return state;
    },
    collapseAll: function () {
       console.log("Collapsing all");
       var i, l;
       for (i = 0, l = this.state.assemblies.length; i < l; i++) {
           this.state.assemblies[i].collapse();
       }

       this.setState({ assemblies: this.state.assemblies });
    },
    buildreporthelper: function (buildinput) {
        this.props.allreport = false;
		nxt = 0;
        var i, l;
        if (buildinput == "on") {
            buildcheck = true;
            for (i = 0, l = this.state.assemblies.length; i < l; i++) {
                this.state.assemblies[i].build();
            }
        }


        this.setState({ assemblies: this.state.assemblies });
    },
    allreporthelper: function (buildinput) {
        this.props.buildreport = false;
		nxt = 0;
        var i, l;
        if (buildinput == "on") {
            buildcheck = false;
            for (i = 0, l = this.state.assemblies.length; i < l; i++) {
                this.state.assemblies[i].build();
            }
        }


        this.setState({ assemblies: this.state.assemblies });
    },
	NextCollapse: function (assembly) {
        assembly.NextCollapse();
        this.setState({ assemblies: this.state.assemblies });
    },
	PrevCollapse: function (assembly) {
        assembly.PrevCollapse();
        this.setState({ assemblies: this.state.assemblies });
    },
    toggleCollapse: function (assembly) {
        assembly.toggleCollapse();
        this.setState({ assemblies: this.state.assemblies });
    },
    updateFilter: function (filter) {
        filter = filter.toLowerCase();

        if (filter === this.state.filter) {
            return;
        }

        console.log("Updating filter: " + filter);
        this.setState({ filter: filter });
    },
    updateSorting: function (sortby) {
        var sortorder = 'asc', assemblies;

        if (sortby === this.state.sortby) {
            sortorder = this.state.sortorder === 'asc' ? 'desc' : 'asc';
        }

        console.log("Updating sorting: " + sortby + ", " + sortorder);
        assemblies = this.getAssemblies(this.props.assemblies, this.state.grouping, sortby, sortorder);
        this.setState({ sortby: sortby, sortorder: sortorder, assemblies: assemblies });
    },
    restoreCollapseState: function (source, target) {
        var i;

        try {
            for (i = 0; i < target.length; i++) {
                if (target[i].isNamespace) {
                    target[i].collapsed = source[i].collapsed;
                    this.restoreCollapseState(source[i].subelements, target[i].subelements)
                }
            }
        } catch (e) {
            // This can only happen if assembly structure was changed.
            // That means the complete report was updated in the background and the reloaded in the same tab/window.
            console.log("Restoring of collapse state failed.");
        }
    },
    extractCollapseState: function (target) {
        var i, currentResult, result = [];

        for (i = 0; i < target.length; i++) {
            if (target[i].isNamespace) {
                currentResult = {
                    collapsed: target[i].collapsed,
                    subelements: this.extractCollapseState(target[i].subelements)

                };
                result.push(currentResult);
            }
        }

        return result;
    },
    render: function () {
        if (window.history !== undefined && window.history.replaceState !== undefined) {
            var historyState, i;
            historyState = angular.copy(this.state);

            historyState.assemblies = this.extractCollapseState(historyState.assemblies);

            window.history.replaceState(historyState, null);
        }

        return (
            React.DOM.div(null,
                SearchBar({
                    groupingMaximum: this.state.groupingMaximum,
                    grouping: this.state.grouping,
                    filter: this.state.filter,
                    collapseAll: this.collapseAll,
                    buildreporthelper: this.buildreporthelper,
                    allreporthelper: this.allreporthelper,
                    ALL: this.ALL,
                    updateFilter: this.updateFilter
                }),
                        AssemblyTable({
                            filter: this.state.filter,
                            assemblies: this.state.assemblies,
                            sortby: this.state.sortby,
                            sortorder: this.state.sortorder,
                            branchCoverageAvailable: this.state.branchCoverageAvailable,
                            updateSorting: this.updateSorting,
                            toggleCollapse: this.toggleCollapse,
							NextCollapse: this.NextCollapse,
							PrevCollapse: this.PrevCollapse
                        }))
        );
    }
});

var SearchBar = React.createClass({
    collapseAllClickHandler: function (event) {
       event.nativeEvent.preventDefault();
       this.props.collapseAll();
    },
    buildreportClickHandler: function () {
        this.refs.buildall.getDOMNode().checked = false;
        this.props.buildreporthelper(this.refs.buildalone.getDOMNode().value);
    },
    allreportClickHandler: function () {
        this.refs.buildalone.getDOMNode().checked = false;
        this.props.allreporthelper(this.refs.buildall.getDOMNode().value);
    },
    filterChangedHandler: function () {
        this.props.updateFilter(this.refs.filterInput.getDOMNode().value);
    },
    render: function () {
        var groupingDescription = translations.byNamespace + ' ' + this.props.grouping;

        if (this.props.grouping === '-1') {
            groupingDescription = translations.noGrouping;
        } else if (this.props.grouping === '0') {
            groupingDescription = translations.byAssembly;
        }

        return (
                    React.DOM.div({ className: 'customizebox' },
                        React.DOM.div(null,
							React.DOM.a({ href: '', onClick: this.collapseAllClickHandler }, translations.collapseAll)),

						React.DOM.div(null,
						React.DOM.input({
						    ref: 'buildalone',
						    type: 'radio',
						    checked: buildcheck,
						    'style': { height: '13px', width: '30px' },
						    value: this.props.buildreport,
						    onChange: this.buildreportClickHandler
						}),
                            React.DOM.span(null, translations.buildreport),
							React.DOM.input({
							    ref: 'buildall',
							    type: 'radio',
							    'style': { height: '13px', width: '30px' },
							    value: this.props.allreport,
							    onChange: this.allreportClickHandler
							}),
							React.DOM.span(null, translations.allreport)
                            ),
                        React.DOM.div({ className: 'right' },
                            React.DOM.span(null, translations.filter + ' '),
                            React.DOM.input({
                                ref: 'filterInput',
                                type: 'text',
                                value: this.props.filter,
                                onChange: this.filterChangedHandler,
                                onInput: this.filterChangedHandler /* Handle text input immediately */
                            })))
                );
    }
});
var AssemblyTable = React.createClass({
    renderAllChilds: function (result, currentElement) {
        var i;
		// currentElement.visible = function (filter) {
        // return filter === '' || self.name.toLowerCase().indexOf(filter) > -1;
		// };
		//currentElement.parent.parent = self;
        if (currentElement.visible(this.props.filter)) {
            if (currentElement.isNamespace) {
                if (buildcheck ? currentElement.build1() : true) {
                    result.push(AssemblyRow({
                        assembly: currentElement,
                        branchCoverageAvailable: this.props.branchCoverageAvailable,
                        toggleCollapse: this.props.toggleCollapse,
						NextCollapse: this.props.NextCollapse,
						PrevCollapse: this.props.PrevCollapse
                    }));
                }
                if (currentElement.collapsed) {
					if (currentElement.cnxt && ctmp) {
						//debugger;
						data = GetMongoData(currentElement.name)
						if (data.rows.length > 0){
							for (nxt, l = data.rows[0].classes.length, m = nxt + inc; nxt >= 0 && nxt < l && nxt < m; nxt++) {
								if (buildcheck ? data.rows[0].classes[nxt].newlines > 0 : true) {
									result.push(ClassRow({clazz: data.rows[0].classes[nxt],branchCoverageAvailable: this.props.branchCoverageAvailable}));
								}
							}
						}
					}
					else if (currentElement.cprv && !ctmp) {
						//debugger;
						data = GetMongoData(currentElement.name)
						if (data.rows.length > 0){
							for (nxt = nxt - inc, l = data.rows[0].classes.length, m = nxt + inc; nxt > 0 && nxt < l && nxt < m; nxt++) {
								if (buildcheck ? data.rows[0].classes[nxt].newlines > 0 : true) {
									result.push(ClassRow({clazz: data.rows[0].classes[nxt],branchCoverageAvailable: this.props.branchCoverageAvailable}));
								}
							}
							if(nxt > 0){
								nxt = nxt - inc;
							}
							else if(nxt < 0){
								nxt = 0;
							}
						}
					}
					else{
						data = GetMongoData(currentElement.name)
						if (data.rows.length > 0){
							for (i = 0, l = data.rows[0].classes.length; i < l && i < inc; i++) {
								if (buildcheck ? data.rows[0].classes[i].newlines > 0 : true) {
									this.renderAllChilds(result, data.rows[0].classes[i]);
								}
							}
						}
						//currentElement.cnxt = false;
					}					
                }		
            } 
			else {
                result.push(ClassRow({
                    clazz: currentElement,
                    branchCoverageAvailable: this.props.branchCoverageAvailable
                }));
            }
        }
    },
    render: function () {
        var rows = [], i, l;

        for (i = 0, l = this.props.assemblies.length; i < l; i++) {
            this.renderAllChilds(rows, this.props.assemblies[i]);
        }

        return (
            React.DOM.table({ className: 'overview table-fixed' },
                React.DOM.colgroup(null,
                    React.DOM.col({ className: 'column250' }),
                    React.DOM.col({ className: 'column80' }),
                    React.DOM.col({ className: 'column100' }), //uncovered
                    React.DOM.col({ className: 'column90' }),
                    React.DOM.col({ className: 'column60' }),
                    React.DOM.col({ className: 'column80' }),
                    React.DOM.col({ className: 'column90' }),
                    React.DOM.col({ className: 'column60' }),
                    React.DOM.col({ className: 'column112' }), //progress bar of build coverage
                    React.DOM.col({ className: 'column60' }),

                    React.DOM.col({ className: 'column112' }), //progress bar of line coverage
                    React.DOM.col({ className: 'column60' }),

                    this.props.branchCoverageAvailable ? React.DOM.col({ className: 'column98' }) : null,
                    this.props.branchCoverageAvailable ? React.DOM.col({ className: 'column112' }) : null),
                TableHeader({
                    sortby: this.props.sortby,
                    sortorder: this.props.sortorder,
                    updateSorting: this.props.updateSorting,
                    branchCoverageAvailable: this.props.branchCoverageAvailable
                }),
                React.DOM.tbody(null, rows))
        );
    }
});
Object.prototype.hasOwnProperty  = function(property) {
    return this[property] !== undefined;
};
function GetMongoDataSum() {
	var MongoAPI = "http://172.26.6.58:28017/CodeData/CodeSum/";
	xhr.open("GET", MongoAPI, false);
	xhr.send();
	data = JSON.parse(xhr.response);
    return data;
};
function GetMongoData(x) {
	var MongoAPI1 = "http://172.26.6.58:28017/CodeData/test_collection/?filter_name=" + x;
	xhr.open("GET", MongoAPI1, false);
	xhr.send();
	data = JSON.parse(xhr.response);
    return data;
};
var TableHeader = React.createClass({
    sortingChangedHandler: function (event, sortby) {
        event.nativeEvent.preventDefault();
        this.props.updateSorting(sortby);
    },

    //IMPORTANT
    render: function () {
        var nameClass = this.props.sortby === 'name' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var coveredClass = this.props.sortby === 'covered' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var uncoveredClass = this.props.sortby === 'uncovered' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var coverableClass = this.props.sortby === 'coverable' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var newlines = this.props.sortby === 'newlines' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var testednewlines = this.props.sortby === 'testednewlines' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';

        var testcoverageClass = this.props.sortby === 'testcoverage' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var totalClass = this.props.sortby === 'total' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var coverageClass = this.props.sortby === 'coverage' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';
        var branchCoverageClass = this.props.sortby === 'branchcoverage' ? 'sortactive' + '_' + this.props.sortorder : 'sortinactive_asc';

        return (
            React.DOM.thead(null,
                React.DOM.tr(null,
                    React.DOM.th(null,
                        React.DOM.a({ className: nameClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'name'); }.bind(this) }, translations.name)),
                    React.DOM.th({ className: 'right' },
                        React.DOM.a({ className: coveredClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'covered'); }.bind(this) }, translations.covered)),
                    React.DOM.th({ className: 'right' },
                        React.DOM.a({ className: uncoveredClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'uncovered'); }.bind(this) }, translations.uncovered)),
                    React.DOM.th({ className: 'right' },
                        React.DOM.a({ className: coverableClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'coverable'); }.bind(this) }, translations.coverable)),
                    React.DOM.th({ className: 'right' },
                        React.DOM.a({ className: totalClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'total'); }.bind(this) }, translations.total)),
                    React.DOM.th({ className: 'right' },
                        React.DOM.a({ className: newlines, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'newlines'); }.bind(this) }, translations.newlines)),
                    React.DOM.th({ className: 'right' },
                        React.DOM.a({ className: testednewlines, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'testednewlines'); }.bind(this) }, translations.testednewlines)),
                        React.DOM.th({ className: 'center', colSpan: '2' },
                        React.DOM.a({ className: testcoverageClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'testcoverage'); }.bind(this) }, translations.testcoverage)),
                   React.DOM.th({ className: 'center', colSpan: '2' },
                        React.DOM.a({ className: coverageClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'coverage'); }.bind(this) }, translations.coverage)),
                    this.props.branchCoverageAvailable ? React.DOM.th({ className: 'center', colSpan: '2' },
                        React.DOM.a({ className: branchCoverageClass, href: '', onClick: function (event) { this.sortingChangedHandler(event, 'branchcoverage'); }.bind(this) }, translations.branchCoverage)) : null))
        );
    }
});

var AssemblyRow = React.createClass({
    toggleCollapseClickHandler: function (event) {
        event.nativeEvent.preventDefault();
        this.props.toggleCollapse(this.props.assembly);
    },
	NextCollapseClickHandler: function (event) {
        event.nativeEvent.preventDefault();
		this.props.NextCollapse(this.props.assembly);
    },
	PrevCollapseClickHandler: function (event) {
        event.nativeEvent.preventDefault();
        this.props.PrevCollapse(this.props.assembly);
    },
	
    //added testcoveragetable
    render: function () {
        var testcoverageTable, testgreenHidden, testredHidden, testgrayHidden, greenHidden, redHidden, grayHidden, coverageTable, branchGreenHidden, branchRedHidden, branchGrayHidden, branchCoverageTable, id;

        testgreenHidden = !isNaN(this.props.assembly.testcoverage) && Math.round(this.props.assembly.testcoverage) > 0 ? '' : ' hidden';
        testredHidden = !isNaN(this.props.assembly.testcoverage) && 100 - Math.round(this.props.assembly.testcoverage) > 0 ? '' : ' hidden';
        testgrayHidden = isNaN(this.props.assembly.testcoverage) ? '' : ' hidden';

        testcoverageTable = React.DOM.table(
            { className: 'testcoverage' },
            React.DOM.tbody(null,
                React.DOM.tr(null,
                    React.DOM.td({ className: 'green covered' + Math.round(this.props.assembly.testcoverage) + testgreenHidden }, ' '),
                    React.DOM.td({ className: 'red covered' + (100 - Math.round(this.props.assembly.testcoverage)) + testredHidden }, ' '),
                    React.DOM.td({ className: 'gray covered100' + testgrayHidden }, ' '))));


        greenHidden = !isNaN(this.props.assembly.coverage) && Math.round(this.props.assembly.coverage) > 0 ? '' : ' hidden';
        redHidden = !isNaN(this.props.assembly.coverage) && 100 - Math.round(this.props.assembly.coverage) > 0 ? '' : ' hidden';
        grayHidden = isNaN(this.props.assembly.coverage) ? '' : ' hidden';

        coverageTable = React.DOM.table(
            { className: 'coverage' },
            React.DOM.tbody(null,
                React.DOM.tr(null,
                    React.DOM.td({ className: 'green covered' + Math.round(this.props.assembly.coverage) + greenHidden }, ' '),
                    React.DOM.td({ className: 'red covered' + (100 - Math.round(this.props.assembly.coverage)) + redHidden }, ' '),
                    React.DOM.td({ className: 'gray covered100' + grayHidden }, ' '))));

        branchGreenHidden = !isNaN(this.props.assembly.coverage) && Math.round(this.props.assembly.branchCoverage) > 0 ? '' : ' hidden';
        branchRedHidden = !isNaN(this.props.assembly.coverage) && 100 - Math.round(this.props.assembly.branchCoverage) > 0 ? '' : ' hidden';
        branchGrayHidden = isNaN(this.props.assembly.branchCoverage) ? '' : ' hidden';

        branchCoverageTable = React.DOM.table(
            { className: 'coverage' },
            React.DOM.tbody(null,
                React.DOM.tr(null,
                    React.DOM.td({ className: 'green covered' + Math.round(this.props.assembly.branchCoverage) + branchGreenHidden }, ' '),
                    React.DOM.td({ className: 'red covered' + (100 - Math.round(this.props.assembly.branchCoverage)) + branchRedHidden }, ' '),
                    React.DOM.td({ className: 'gray covered100' + branchGrayHidden }, ' '))));

        id = '_' + createRandomId(8);

        return (
          React.DOM.tr({ className: this.props.assembly.parent !== null ? 'namespace' : null },
            React.DOM.th(null,
                React.DOM.a(
                        {
                            id: this.props.assembly.name + id,
                            href: '',
                            onClick: this.toggleCollapseClickHandler,
                            className: (translations.asmbly == "Summary View") ? this.props.assembly.collapsed ? 'expanded' : 'collapsed' : 'disabled'
                        },
                        this.props.assembly.name),
				React.DOM.br(),
				React.DOM.a(
                {
                    id: this.props.assembly.name + id + "1",
                    href: '',
                    onClick: this.PrevCollapseClickHandler,
                    className: (this.props.assembly.collapsed)? ((nxt > inc)? 'Prev' : 'disabled') : 'disabled'
                }),
				React.DOM.a(
                {
                    id: this.props.assembly.name + id + "2",
                    href: '',
                    onClick: this.NextCollapseClickHandler,
                    className: (this.props.assembly.collapsed )? ((l > nxt)? 'Next' : 'disabled') : 'disabled'
                })),
            React.DOM.th({ className: 'right' }, this.props.assembly.coveredLines),
            React.DOM.th({ className: 'right' }, this.props.assembly.uncoveredLines),
            React.DOM.th({ className: 'right' }, this.props.assembly.coverableLines),
            React.DOM.th({ className: 'right' }, this.props.assembly.totalLines),
            React.DOM.th({ className: 'right' }, this.props.assembly.newlines),
            React.DOM.th({ className: 'right' }, this.props.assembly.testednewlines),
            React.DOM.th(
                    {
                        className: 'right',
                        title: isNaN(this.props.assembly.testcoverage) ? '' : this.props.assembly.coverageType
                    },
                    isNaN(this.props.assembly.testcoverage) ? '' : this.props.assembly.testcoverage + '%'),
             React.DOM.th(null, testcoverageTable),

            React.DOM.th(
                    {
                        className: 'right',
                        title: isNaN(this.props.assembly.coverage) ? '' : this.props.assembly.coverageType
                    },
                    isNaN(this.props.assembly.coverage) ? '' : this.props.assembly.coverage + '%'),
            React.DOM.th(null, coverageTable),
            this.props.branchCoverageAvailable ? React.DOM.th(
                    {
                        className: 'right'
                    },
                    isNaN(this.props.assembly.branchCoverage) ? '' : this.props.assembly.branchCoverage + '%') : null,
            this.props.branchCoverageAvailable ? React.DOM.th(null, branchCoverageTable) : null)
        );
    }
});

var ClassRow = React.createClass({
    render: function ()
        //added testcoveragetable
    {
        var nameElement, testcoverageTable, testgreenHidden, testredHidden, testgrayHidden, greenHidden, redHidden, grayHidden, coverageTable, branchGreenHidden, branchRedHidden, branchGrayHidden, branchCoverageTable;

        if (this.props.clazz.reportPath === '') {
            nameElement = React.DOM.span(null, this.props.clazz.name);
        } else {
            nameElement = React.DOM.a({ href: this.props.clazz.reportPath }, this.props.clazz.name);
        }
        //changed
        testgreenHidden = !isNaN(this.props.clazz.testcoverage) && Math.round(this.props.clazz.testcoverage) > 0 ? '' : ' hidden';
        testredHidden = !isNaN(this.props.clazz.testcoverage) && 100 - Math.round(this.props.clazz.testcoverage) > 0 ? '' : ' hidden';
        testgrayHidden = isNaN(this.props.clazz.testcoverage) ? '' : ' hidden';

        testcoverageTable = React.DOM.table(
            { className: 'coverage' },
            React.DOM.tbody(null,
                React.DOM.tr(null,
                    React.DOM.td({ className: 'green covered' + Math.round(this.props.clazz.testcoverage) + testgreenHidden }, ' '),
                    React.DOM.td({ className: 'red covered' + (100 - Math.round(this.props.clazz.testcoverage)) + testredHidden }, ' '),
                    React.DOM.td({ className: 'gray covered100' + testgrayHidden }, ' '))));

        greenHidden = !isNaN(this.props.clazz.coverage) && Math.round(this.props.clazz.coverage) > 0 ? '' : ' hidden';
        redHidden = !isNaN(this.props.clazz.coverage) && 100 - Math.round(this.props.clazz.coverage) > 0 ? '' : ' hidden';
        grayHidden = isNaN(this.props.clazz.coverage) ? '' : ' hidden';

        coverageTable = React.DOM.table(
            { className: 'coverage' },
            React.DOM.tbody(null,
                React.DOM.tr(null,
                    React.DOM.td({ className: 'green covered' + Math.round(this.props.clazz.coverage) + greenHidden }, ' '),
                    React.DOM.td({ className: 'red covered' + (100 - Math.round(this.props.clazz.coverage)) + redHidden }, ' '),
                    React.DOM.td({ className: 'gray covered100' + grayHidden }, ' '))));

        branchGreenHidden = !isNaN(this.props.clazz.branchCoverage) && Math.round(this.props.clazz.branchCoverage) > 0 ? '' : ' hidden';
        branchRedHidden = !isNaN(this.props.clazz.branchCoverage) && 100 - Math.round(this.props.clazz.branchCoverage) > 0 ? '' : ' hidden';
        branchGrayHidden = isNaN(this.props.clazz.branchCoverage) ? '' : ' hidden';

        branchCoverageTable = React.DOM.table(
            { className: 'coverage' },
            React.DOM.tbody(null,
                React.DOM.tr(null,
                    React.DOM.td({ className: 'green covered' + Math.round(this.props.clazz.branchCoverage) + branchGreenHidden }, ' '),
                    React.DOM.td({ className: 'red covered' + (100 - Math.round(this.props.clazz.branchCoverage)) + branchRedHidden }, ' '),
                    React.DOM.td({ className: 'gray covered100' + branchGrayHidden }, ' '))));

        return (
            React.DOM.tr({ className: this.props.clazz !== null ? 'namespace' : null },
                React.DOM.td(null, nameElement),
                React.DOM.td({ className: 'right' }, this.props.clazz.coveredLines),
                React.DOM.td({ className: 'right' }, this.props.clazz.uncoveredLines),
                React.DOM.td({ className: 'right' }, this.props.clazz.coverableLines),
                React.DOM.td({ className: 'right' }, this.props.clazz.totalLines),
                React.DOM.td({ className: 'right' }, this.props.clazz.newlines),
                React.DOM.td({ className: 'right' }, this.props.clazz.testednewlines),

            React.DOM.td({ className: 'right', title: this.props.clazz.testcoverageTitle },
                    CoverageHistoryChart({
                        historicCoverage: this.props.clazz.lineCoverageHistory,
                        cssClass: 'tinylinecoveragechart',
                        title: translations.history + ": " + translations.testcoverage,
                        id: 'chart' + createRandomId(8)
                    }),
                    this.props.clazz.testcoveragePercent),
                React.DOM.td(null, testcoverageTable),

                React.DOM.td({ className: 'right', title: this.props.clazz.coverageTitle },
                    CoverageHistoryChart({
                        historicCoverage: this.props.clazz.lineCoverageHistory,
                        cssClass: 'tinylinecoveragechart',
                        title: translations.history + ": " + translations.coverage,
                        id: 'chart' + createRandomId(8)
                    }),
                    this.props.clazz.coveragePercent),
                React.DOM.td(null, coverageTable),
                this.props.branchCoverageAvailable ? React.DOM.td({ className: 'right' },
                    CoverageHistoryChart({
                        historicCoverage: this.props.clazz.branchCoverageHistory,
                        cssClass: 'tinybranchcoveragechart',
                        title: translations.history + ": " + translations.branchCoverage,
                        id: 'chart' + createRandomId(8)
                    }),
                    this.props.clazz.branchCoveragePercent) : null,
                this.props.branchCoverageAvailable ? React.DOM.td(null, branchCoverageTable) : null)
        );
    }
});

var CoverageHistoryChart = React.createClass({
    updateChart: function () {
        if (this.props.historicCoverage.length <= 1) {
            return;
        }

        new Chartist.Line('#' + this.props.id, {
            labels: [],
            series: [this.props.historicCoverage]
        }, {
            axisX: {
                offset: 0,
                showLabel: false,
                showGrid: false
            },
            axisY: {
                offset: 0,
                showLabel: false,
                showGrid: false,
                scaleMinSpace: 0.1
            },
            showPoint: false,
            chartPadding: 0,
            lineSmooth: false,
            low: 0,
            high: 100,
            fullWidth: true,
        });
    },
    componentDidMount: function () {
        this.updateChart();
    },
    componentDidUpdate: function () {
        this.updateChart();
    },
    render: function () {
        if (this.props.historicCoverage.length <= 1) {
            return (
                React.DOM.div(
                {
                    id: this.props.id,
                    className: 'hidden',
                })
            );
        } else {
            return (
                React.DOM.div(
                {
                    id: this.props.id,
                    className: this.props.cssClass + ' ct-chart',
                    title: this.props.title
                })
            );
        }
    }
});