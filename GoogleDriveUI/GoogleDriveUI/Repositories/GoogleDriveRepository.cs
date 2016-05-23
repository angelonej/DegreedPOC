using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Drive.v3.Data;
using System.Collections;

namespace GoogleDriveUI.Repositories
{
    public class GoogleDriveConfig 
    {
        public long OrganizationId { get; }
        public string AppName { get; set; }
        public string Url { get; }
        public string ApiKey { get; }
        public string AccessToken { get; }
        public string RefreshToken { get; }
        public Dictionary<string, bool> Folders { get; }
        public string UserName { get; set; }
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string Password { get; }

        public GoogleDriveConfig(long organizationId, string url, string appName, string apiKey, string accessToken, string refreshToken, string clientId, string clientSecret, string userName, Dictionary<string, bool> folders)
        {
            OrganizationId = organizationId;
            Url = url;
            AppName = appName;
            ApiKey = apiKey;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            ClientId = clientId;
            ClientSecret = clientSecret;
            Folders = folders;
            UserName = userName;
        }
    }

    public class GoogleDriveItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsVideo { get; set; }
        public bool IsFolder { get; set; }
        public string WebLink { get; set; }
        public string Description { get; set; }
        public string MimeType { get; set; }
        public IList<String> Parents { get; set; }
        public IEnumerable<String> Tags { get; set; }
        public IList<GoogleDriveItem> Children { get; set; }
        public Boolean Checked { get; set; }
    }


    public class GoogleDriveRepository
    {
        private DriveService _client;
        private GoogleDriveConfig _config;
        private string _rootId = "";

        // all items on drive
        private List<Google.Apis.Drive.v3.Data.File> _allItems;

        // content to show
        private List<GoogleDriveItem> _allFiles = new List<GoogleDriveItem>();


        public GoogleDriveRepository(GoogleDriveConfig config)
        {
            _config = config;
            _InitializeClient();
            _RetrieveAllFilesFromFolders();
        }


        public List<GoogleDriveItem> GetArticles()
        {
            return _allFiles.Where(item => !item.IsVideo && !item.IsFolder).ToList();
        }


        public List<GoogleDriveItem> GetVideos()
        {
            return _allFiles.Where(item => item.IsVideo).ToList();
        }

        public List<GoogleDriveItem> GetChildFolders(string folderName)
        {
            return _GetFolderContents(folderName, false).Where(f => f.IsFolder).ToList();
        }

        public List<GoogleDriveItem> GetHeirarchy()
        {
            return _BuildHeirarchy().OrderBy(o => o.Name).ToList(); // _GetFolderContents("root", true).Where(f => f.IsFolder).OrderBy(o => o.Name).ToList();
        }

        private void _InitializeClient()
        {
            _client = _CreateService();

            var getRequest = _client.Files.Get("root");
            getRequest.Fields = "id";
            var file = getRequest.Execute();
            _rootId = file.Id;

        }


        private DriveService _CreateService()
        {
            var tokenResponse = new TokenResponse
            {
                AccessToken = _config.AccessToken,
                RefreshToken = _config.RefreshToken,
            };

            var apiCodeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _config.ClientId,
                    ClientSecret = _config.ClientSecret
                },
                Scopes = new[] { DriveService.Scope.Drive },
                DataStore = new FileDataStore(_config.AppName)
            });

            var credential = new Google.Apis.Auth.OAuth2.UserCredential(apiCodeFlow, _config.UserName, tokenResponse);

            var service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _config.AppName
            });

            return service;
        }


        private List<Google.Apis.Drive.v3.Data.File> _RetrieveAllItems()
        {
            List<Google.Apis.Drive.v3.Data.File> result = new List<Google.Apis.Drive.v3.Data.File>();
            FilesResource.ListRequest request = _client.Files.List();
            request.Q = "trashed=false ";
            request.Fields = "nextPageToken, files(id, name, webViewLink, description, mimeType, parents)";

            do
            {
                try
                {
                    FileList files = request.Execute();

                    result.AddRange(files.Files);
                    request.PageToken = files.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    request.PageToken = null;
                }
            } while (!String.IsNullOrEmpty(request.PageToken));

            return result;
        }

        private List<GoogleDriveItem> _BuildHeirarchy()
        {
            // non recursive solution. recursive would be a bit more elegant
            var allFolders = _allItems.Where(f => f.Parents != null && f.MimeType == "application/vnd.google-apps.folder").ToList();
          

            // loop thru folders, add all child folders
            var googleFolders = new List<GoogleDriveItem>();
            foreach (var folder in allFolders)
            {
                var item = _ToGoogleDriveItem(folder);

                // get children
                var childItems = _allItems.Where(f => f.Parents != null && f.MimeType == "application/vnd.google-apps.folder").Where(f2 => f2.Parents.Contains(folder.Id)).ToList();
                var childList = new List<GoogleDriveItem>();

                foreach (var child in childItems)
                {
                    childList.Add(_ToGoogleDriveItem(child));
                }
                item.Children = childList;

                // add to folder list
                googleFolders.Add(item);
            }


            // process all folders that have children
            var stack = new Stack<GoogleDriveItem>();
            foreach (var folder in googleFolders)
            {
                if (folder.Parents != null && folder.Children.Count > 0)
                {
                    var parentId = folder.Parents[0];
                    if (parentId != _rootId)
                         stack.Push(folder);
                }
            }



            //process stack
            while (stack.Any())
            {
                // process stack item
                GoogleDriveItem node = stack.Pop();
                var parent = googleFolders.FirstOrDefault(f => f.Id == node.Parents[0]);

                // remove other children with same id
                var child = parent.Children.FirstOrDefault(f => f.Id == node.Id);
                if (child != null)
                    parent.Children.Remove(child);

                // add current node
                parent.Children.Add(node);
            }

            // return root level folders, all other folders have been moved under subfolders
            var rc = googleFolders.Where(f => f.Parents.Contains(_rootId)).ToList();
            return rc;
        }


        private List<GoogleDriveItem> _BuildHeirarchyRecursive()
        {
            var returnList = new List<GoogleDriveItem>();
            var folderId = "";

            var getRequest = _client.Files.Get("root");
            getRequest.Fields = "id";
            var file = getRequest.Execute();
            folderId = file.Id;

            // build list of all folders
            var allFolders = _allItems.Where(f => f.Parents != null && f.MimeType == "application/vnd.google-apps.folder").ToList();

            // loop thru folders, add all child folders
            var googleFolders = new List<GoogleDriveItem>();
            foreach (var folder in allFolders)
            {
                var item = _ToGoogleDriveItem(folder);

                var childItems = _allItems.Where(f => f.Parents != null && f.MimeType == "application/vnd.google-apps.folder").Where(f2 => f2.Parents.Contains(folder.Id)).ToList();
                var childList = new List<GoogleDriveItem>();
                foreach (var child in childItems)
                {
                    childList.Add(_ToGoogleDriveItem(child));
                }
                item.Children = childList;

                googleFolders.Add(item);
            }

            return googleFolders.Where(f => f.Parents.Contains(folderId)).ToList();
        }

        private void _RetrieveAllFilesFromFolders()
        {
            // get list of all items on drive
            _allItems = _RetrieveAllItems();

            // parse config folders and extract files to include
           
            var folders = _config.Folders;
            if (folders != null)
            {
                foreach (var folder in folders.Keys)
                {
                    var files = _GetFolderContents(folder, folders[folder]);
                    _allFiles.AddRange(files);
                }
            }
        }



        private List<GoogleDriveItem> _GetFolderContents(string folderName, bool includeSubFolders = true)
        {
            var returnList = new List<GoogleDriveItem>();


                var folderId = "";

            if (folderName == "root")
            {
                //var getRequest = _client.Files.Get(folderName);
                //getRequest.Fields = "id";
                //var file = getRequest.Execute();
                folderId = _rootId; // file.Id;
            }
            else
            {
                folderId = _allItems.Where(f => f.Name.ToLower() == folderName.ToLower() && f.MimeType == "application/vnd.google-apps.folder").First().Id;
            }


            //var folderContents = _allItems.Where(f => f.Parents != null).Where(f2 => f2.Parents.Contains(folderId)).ToList();
            var folderContents = _allItems.Where(f => f.Parents != null).Where(f2 => f2.Parents.Contains(folderId)).ToList();
            var childFolders = _allItems.Where(f => f.Parents != null && f.MimeType == "application/vnd.google-apps.folder").Where(f2 => f2.Parents.Contains(folderId)).ToList();

            foreach (var file in childFolders)
            {
                //List<string> metdata = file.Description?.ToLower().Split('|').ToList();

                //// parse title, tags, and description from description field
                //var title = metdata?.Where(m => m.StartsWith("title")).FirstOrDefault().Split(':')[1];
                //var tags = metdata?.Where(m => m.StartsWith("tags")).FirstOrDefault().Split(':')[1];
                //var desc = metdata?.Where(m => m.StartsWith("description")).FirstOrDefault().Split(':')[1];

                //var googleDriveItem = new GoogleDriveItem
                //{
                //    Id = file.Id,
                //    Name = file.Name,
                //    Title = title ?? file.Name,
                //    Tags = tags?.Split(',').Select(t => t),
                //    Description = desc ?? file.Description,
                //    Url = file.WebViewLink,
                //    IsVideo = file.MimeType.Contains("video"),
                //    IsFolder = file.MimeType.Equals("application/vnd.google-apps.folder"),
                //    Parents = file.Parents,
                //};
                //googleDriveItem.Children = childFolders;

                var item = _ToGoogleDriveItem(file);
                if (item.IsFolder)
                {
                    var folders = _allItems.Where(f => f.Parents != null && f.MimeType == "application/vnd.google-apps.folder").Where(f2 => f2.Parents.Contains(file.Id)).ToList();

                    var folderList = new List<GoogleDriveItem>();
                    foreach (var folder in folders)
                    {
                        folderList.Add(_ToGoogleDriveItem(folder));
                    }
                    item.Children = _GetFolderContents(item.Name, includeSubFolders); // folderList;
                }

                returnList.Add(item);
            }

            

            //if (includeSubFolders)
            //{
            //    foreach (var folder in childFolders)
            //    {
            //        returnList.AddRange(_GetFolderContents(folder.Name, includeSubFolders));
            //    }
            //}

            return returnList;
        }

        private GoogleDriveItem _ToGoogleDriveItem(Google.Apis.Drive.v3.Data.File file)
        {
            List<string> metdata = file.Description?.ToLower().Split('|').ToList();

            string title = null;
            string tags = null;
            string desc = null;

            // parse title, tags, and description from description field

            if (metdata?.Count() > 1)
            {
                 title = metdata?.Where(m => m.StartsWith("title")).FirstOrDefault().Split(':')[1];
                 tags = metdata?.Where(m => m.StartsWith("tags")).FirstOrDefault().Split(':')[1];
                 desc = metdata?.Where(m => m.StartsWith("description")).FirstOrDefault().Split(':')[1];

            }
            var googleDriveItem = new GoogleDriveItem
            {
                Id = file.Id,
                Name = file.Name,
                Title = title ?? file.Name,
                Tags = tags?.Split(',').Select(t => t),
                Description = desc ?? file.Description,
                Url = file.WebViewLink,
                IsVideo = file.MimeType.Contains("video"),
                IsFolder = file.MimeType.Equals("application/vnd.google-apps.folder"),
                Parents = file.Parents,
            };
            

            // googleDriveItem.Children = childFolders;

            return googleDriveItem;
        }
    }





    //public ArticleMetaData ToArticle(GoogleDriveItem item, long organizationId)
    //{
    //    var article = new ArticleMetaData
    //    {
    //        Title = Core.Helpers.StringHelper.CleanHtml(item.Title),
    //        ProviderCode = ProviderCode,
    //        Summary = Core.Helpers.StringHelper.CleanHtml(item.Description),
    //        Url = item.Url,
    //        ImageUrl = null,
    //        SourceName = null,
    //        ExternalId = item.Id,
    //        ExternalData = null,
    //        InputTypeId = (byte)InputTypes.Article,
    //        MediaLength = 0,
    //        OrganizationId = organizationId.ToString(),
    //        Tags = item.Tags,
    //    };
    //    return article;
    //}


    //public VideoMetaData ToVideo(GoogleDriveItem item, long organizationId)
    //{
    //    var video = new VideoMetaData
    //    {
    //        videoProviderCode = ProviderCode,
    //        videoSourceName = null,
    //        id = item.Id,
    //        title = item.Title,
    //        description = Core.Helpers.StringHelper.CleanHtml(item.Description),
    //        url = item.Url,
    //        image_url = null,
    //        duration = 5 * 60,
    //        views = 0,
    //        keywords = item.Tags.ToString(),
    //        ka_url = null,
    //        readable_id = null,
    //        youtube_id = null,
    //        download_urls = null,
    //    };
    //    return video;
    //}
}



