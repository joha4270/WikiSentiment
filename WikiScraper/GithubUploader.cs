using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;


namespace WikiScraper
{
    internal class GithubUploader
    {
        GitHubClient client;

        const string owner = "holyhamster";
        const string repo = "azure_angular_app";
        const string branch = "dev";
        const string targetFile = "src/assets/test.txt"; //todo move to config

        public GithubUploader(string _token)
        {
            client = new GitHubClient(new ProductHeaderValue("wiki-sentiment"))
            {
                Credentials = new Credentials(_token)
            };
        }

        //fetches current version of the database from github
        public async Task<string> GetDB()
        {
            try
            {
                // try to get the file
                var existingFile = await client.Repository.Content.GetAllContentsByRef(owner, repo, targetFile, branch);
                return existingFile.First().Content;
            }
            catch (NotFoundException)
            {
                return "";
            }
        }

        public async Task Upload(string _content)
        {
            _content = "export const WikiData =" + _content;
            var owner = "holyhamster";
            var repo = "azure_angular_app";
            var branch = "dev";


            var targetFile = "src/assets/WikiData.json.ts";

            try
            {
                // try to get the file (and with the file the last commit sha)
                var existingFile = await client.Repository.Content.GetAllContentsByRef(owner, repo, targetFile, branch);

                // update the file
                var updateChangeSet = await client.Repository.Content.UpdateFile(owner, repo, targetFile,
                   new UpdateFileRequest("API File update: " + DateTime.UtcNow, _content, existingFile.First().Sha, branch));
            }
            catch (Octokit.NotFoundException)
            {
                // if file is not found, create it
                var createChangeSet = await client.Repository.Content.CreateFile(owner, repo, targetFile, 
                    new CreateFileRequest("API File update: " + DateTime.UtcNow, _content, branch));
            }
            return;
        }
    }
}
