
(function (ng) {
    var app = ng.module('tree.directives', []);
    app.directive('nodeTree', function () {
        return {
            template: '<node ng-repeat="node in tree"></node>',
            replace: true,
            restrict: 'E',
            scope: {
                tree: '=children'
            }
        };
    });
    app.directive('node', function ($compile) {
        return {
            restrict: 'E',
            replace: true,
            templateUrl: 'content/partials/node.html', // HTML for a single node.
            link: function (scope, element) {
                /*
                 * Here we are checking that if current node has children then compiling/rendering children.
                 * */
                if (scope.node && scope.node.Children && scope.node.Children.length > 0) {
                    scope.node.childrenVisibility = true;
                    var childNode = $compile('<ul class="tree" ng-if="!node.childrenVisibility"><node-tree children="node.Children"></node-tree></ul>')(scope);
                    element.append(childNode);
                } else {
                    scope.node.childrenVisibility = false;
                }
                scope.node.expanded = false;
            },
            controller: ["$scope", function ($scope) {
                // This function is for just toggle the visibility of children
                $scope.toggleVisibility = function (node) {
                    if (node.Children.length > 0) {
                        node.childrenVisibility = !node.childrenVisibility;
                        node.expanded = !node.expanded;
                    }
                };
                // Here We are marking check/un-check all the nodes.
                $scope.checkNode = function (node) {
                    node.Checked = !node.Checked;
                    function checkChildren(c) {
                        angular.forEach(c.Children, function (c) {
                            c.Checked = node.Checked;
                            checkChildren(c);
                        });
                    }

                    checkChildren(node);
                };
            }]
        };
    });
})(angular);