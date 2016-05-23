
(function (ng) {
    var treeServiceApp = ng.module('tree.service', ['tree.factory']);
    treeServiceApp.service("TreeService", ["$http", "URLConfig", function ($http, URLConfig) {
        this.getTree = function () {
            return $http.get(URLConfig.tree);
        };
    }]);
})(angular);
